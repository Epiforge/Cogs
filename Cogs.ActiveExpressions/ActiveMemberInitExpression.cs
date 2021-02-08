using Cogs.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Cogs.ActiveExpressions
{
    class ActiveMemberInitExpression : ActiveExpression, IEquatable<ActiveMemberInitExpression>
    {
        public ActiveMemberInitExpression(MemberInitExpression memberInitExpression, ActiveExpressionOptions? options, CachedInstancesKey<MemberInitExpression> instancesKey, bool deferEvaluation) : base(memberInitExpression.Type, memberInitExpression.NodeType, options, deferEvaluation)
        {
            if (memberInitExpression.NewExpression.Type.IsValueType)
                throw new NotSupportedException("Member initialization expressions of value types are not supported");
            var memberAssignmentExpressions = new Dictionary<ActiveExpression, MemberInfo>();
            try
            {
                this.memberInitExpression = memberInitExpression;
                this.instancesKey = instancesKey;
                newExpression = Create(memberInitExpression.NewExpression, options, deferEvaluation);
                newExpression.PropertyChanged += NewExpressionPropertyChanged;
                foreach (var binding in memberInitExpression.Bindings)
                {
                    if (binding is MemberAssignment memberAssignmentBinding)
                    {
                        var memberAssignmentExpression = Create(memberAssignmentBinding.Expression, options, deferEvaluation);
                        memberAssignmentExpressions.Add(memberAssignmentExpression, memberAssignmentBinding.Member);
                        memberAssignmentExpression.PropertyChanged += MemberAssignmentExpressionPropertyChanged;
                    }
                    else
                        throw new NotSupportedException("Only member assignment bindings are supported in member init expressions");
                }
                this.memberAssignmentExpressions = memberAssignmentExpressions.ToImmutableDictionary();
                EvaluateIfNotDeferred();
            }
            catch (Exception ex)
            {
                DisposeValueIfNecessaryAndPossible();
                if (newExpression is not null)
                {
                    newExpression.PropertyChanged -= NewExpressionPropertyChanged;
                    newExpression.Dispose();
                }
                foreach (var memberAssignmentExpression in memberAssignmentExpressions.Keys)
                {
                    memberAssignmentExpression.PropertyChanged -= MemberAssignmentExpressionPropertyChanged;
                    memberAssignmentExpression.Dispose();
                }
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        int disposalCount;
        readonly CachedInstancesKey<MemberInitExpression> instancesKey;
        readonly IReadOnlyDictionary<ActiveExpression, MemberInfo> memberAssignmentExpressions;
        readonly MemberInitExpression memberInitExpression;
        readonly ActiveExpression newExpression;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove(instancesKey);
                    result = true;
                }
            if (result)
            {
                DisposeValueIfNecessaryAndPossible();
                if (newExpression is not null)
                {
                    newExpression.PropertyChanged -= NewExpressionPropertyChanged;
                    newExpression.Dispose();
                }
                foreach (var memberAssignmentExpression in memberAssignmentExpressions.Keys)
                {
                    memberAssignmentExpression.PropertyChanged -= MemberAssignmentExpressionPropertyChanged;
                    memberAssignmentExpression.Dispose();
                }
            }
            return result;
        }

        public override bool Equals(object? obj) => obj is ActiveMemberInitExpression other && Equals(other);

        public bool Equals(ActiveMemberInitExpression other) => newExpression.Equals(other.newExpression) && memberAssignmentExpressions.Select(kv => (memberName: kv.Value.Name, expression: kv.Key)).OrderBy(t => t.memberName).SequenceEqual(other.memberAssignmentExpressions.Select(kv => (memberName: kv.Value.Name, expression: kv.Key)).OrderBy(t => t.memberName)) && Equals(options, other.options);

        protected override void Evaluate()
        {
            try
            {
                var newFault = newExpression.Fault;
                var memberAssignmentFault = memberAssignmentExpressions.Keys.Select(memberAssignmentExpression => memberAssignmentExpression.Fault).Where(fault => fault is not null).FirstOrDefault();
                if (newFault is not null)
                    Fault = newFault;
                else if (memberAssignmentFault is not null)
                    Fault = memberAssignmentFault;
                else
                {
                    var val = newExpression.Value;
                    foreach (var kv in memberAssignmentExpressions)
                    {
                        if (kv.Value is FieldInfo field)
                            field.SetValue(val, kv.Key.Value);
                        else if (kv.Value is PropertyInfo property)
                            FastMethodInfo.Get(property.SetMethod).Invoke(val, new object?[] { kv.Key.Value });
                        else
                            throw new NotSupportedException("Cannot handle member that is not a field or property");
                    }
                    Value = val;
                }
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode()
        {
            var hashCode = HashCode.Combine(typeof(ActiveMemberInitExpression), newExpression, options);
            foreach (var memberAssignmentExpression in memberAssignmentExpressions)
                hashCode = HashCode.Combine(hashCode, memberAssignmentExpression.Key, memberAssignmentExpression.Value);
            return hashCode;
        }

        protected override bool GetShouldValueBeDisposed() => ApplicableOptions.IsConstructedTypeDisposed(memberInitExpression.NewExpression.Constructor);

        void MemberAssignmentExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Evaluate();
            else if (e.PropertyName == nameof(Value))
            {
                if (TryGetUndeferredValue(out var val) && val is not null && sender is ActiveExpression memberAssignmentExpression)
                {
                    var member = memberAssignmentExpressions[memberAssignmentExpression];
                    if (member is FieldInfo field)
                        field.SetValue(val, memberAssignmentExpression.Value);
                    else if (member is PropertyInfo property)
                        FastMethodInfo.Get(property.SetMethod).Invoke(val, new object?[] { memberAssignmentExpression.Value });
                    else
                        throw new NotSupportedException("Cannot handle member that is not a field or property");
                }
                else
                    Evaluate();
            }
        }

        void NewExpressionPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        public override string ToString() => $"{newExpression} {{ {string.Join(", ", memberAssignmentExpressions.Select(kv => $"{kv.Value.Name} = {kv.Key}"))} }} {ToStringSuffix}";

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<CachedInstancesKey<MemberInitExpression>, ActiveMemberInitExpression> instances = new Dictionary<CachedInstancesKey<MemberInitExpression>, ActiveMemberInitExpression>(new CachedInstancesKeyComparer<MemberInitExpression>());

        public static ActiveMemberInitExpression Create(MemberInitExpression memberInitExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var key = new CachedInstancesKey<MemberInitExpression>(memberInitExpression, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeMemberInitExpression))
                {
                    activeMemberInitExpression = new ActiveMemberInitExpression(memberInitExpression, options, key, deferEvaluation);
                    instances.Add(key, activeMemberInitExpression);
                }
                ++activeMemberInitExpression.disposalCount;
                return activeMemberInitExpression;
            }
        }

        public static bool operator ==(ActiveMemberInitExpression a, ActiveMemberInitExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveMemberInitExpression a, ActiveMemberInitExpression b) => !(a == b);
    }
}
