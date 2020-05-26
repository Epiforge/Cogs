using Cogs.Collections;
using Cogs.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Cogs.ActiveExpressions
{
    class ActiveMemberExpression : ActiveExpression, IEquatable<ActiveMemberExpression>
    {
        ActiveMemberExpression(Type type, ActiveExpression expression, MemberInfo member, ActiveExpressionOptions? options, bool deferEvaluation) : base(type, ExpressionType.MemberAccess, options, deferEvaluation)
        {
            this.expression = expression;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
            this.member = member;
            switch (this.member)
            {
                case FieldInfo field:
                    this.field = field;
                    break;
                case PropertyInfo property:
                    getMethod = property.GetMethod;
                    fastGetter = FastMethodInfo.Get(getMethod);
                    break;
            }
            EvaluateIfNotDeferred();
        }

        ActiveMemberExpression(Type type, MemberInfo member, ActiveExpressionOptions? options, bool deferEvaluation) : base(type, ExpressionType.MemberAccess, options, deferEvaluation)
        {
            this.member = member;
            switch (this.member)
            {
                case FieldInfo field:
                    this.field = field;
                    break;
                case PropertyInfo property:
                    getMethod = property.GetMethod;
                    fastGetter = FastMethodInfo.Get(getMethod);
                    break;
            }
            EvaluateIfNotDeferred();
        }

        int disposalCount;
        readonly ActiveExpression? expression;
        object? expressionValue;
        readonly FastMethodInfo? fastGetter;
        readonly FieldInfo? field;
        readonly MethodInfo? getMethod;
        readonly MemberInfo member;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    if (expression is { })
                        instanceInstances.Remove((expression, member, options));
                    else
                        staticInstances.Remove((member, options));
                    result = true;
                }
            if (result)
            {
                DisposeValueIfNecessaryAndPossible();
                if (fastGetter is { })
                    UnsubscribeFromExpressionValueNotifications();
                else if (field is { })
                    UnsubscribeFromValueNotifications();
                if (expression is { })
                {
                    expression.PropertyChanged -= ExpressionPropertyChanged;
                    expression.Dispose();
                }
            }
            return result;
        }

        public override bool Equals(object? obj) => obj is ActiveMemberExpression other && Equals(other);

        public bool Equals(ActiveMemberExpression other) => Equals(expression, other.expression) && member == other.member && Equals(options, other.options);

        protected override void Evaluate()
        {
            try
            {
                var expressionFault = expression?.Fault;
                if (expressionFault is { })
                    Fault = expressionFault;
                else
                {
                    if (fastGetter is { })
                    {
                        var newExpressionValue = expression?.Value;
                        if (newExpressionValue != expressionValue)
                        {
                            UnsubscribeFromExpressionValueNotifications();
                            expressionValue = newExpressionValue;
                            SubscribeToExpressionValueNotifications();
                        }
                        Value = fastGetter.Invoke(expressionValue, emptyArray);
                    }
                    else if (field is { })
                    {
                        UnsubscribeFromValueNotifications();
                        Value = field.GetValue(expression?.Value);
                        SubscribeToValueNotifications();
                    }
                }
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        void ExpressionValuePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == member.Name)
                Evaluate();
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveMemberExpression), expression, member, options);

        protected override bool GetShouldValueBeDisposed() => getMethod is { } && ApplicableOptions.IsMethodReturnValueDisposed(getMethod);

        public override string ToString() => $"{expression?.ToString() ?? member.DeclaringType.FullName}.{member.Name} {ToStringSuffix}";

        void SubscribeToExpressionValueNotifications()
        {
            if (expressionValue is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged += ExpressionValuePropertyChanged;
        }

        void SubscribeToValueNotifications()
        {
            if (field is { })
            {
                if (ApplicableOptions.MemberExpressionsListenToFieldValuesForDictionaryChanged && Value is INotifyDictionaryChanged dictionaryChangedNotifier)
                    dictionaryChangedNotifier.DictionaryChanged += ValueChanged;
                else if (ApplicableOptions.MemberExpressionsListenToFieldValuesForCollectionChanged && Value is INotifyCollectionChanged collectionChangedNotifier)
                    collectionChangedNotifier.CollectionChanged += ValueChanged;
            }
        }

        void UnsubscribeFromExpressionValueNotifications()
        {
            if (expressionValue is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged -= ExpressionValuePropertyChanged;
        }

        void UnsubscribeFromValueNotifications()
        {
            if (field is { } && TryGetUndeferredValue(out var value))
            {
                if (ApplicableOptions.MemberExpressionsListenToFieldValuesForDictionaryChanged && value is INotifyDictionaryChanged dictionaryChangedNotifier)
                    dictionaryChangedNotifier.DictionaryChanged -= ValueChanged;
                else if (ApplicableOptions.MemberExpressionsListenToFieldValuesForCollectionChanged && value is INotifyCollectionChanged collectionChangedNotifier)
                    collectionChangedNotifier.CollectionChanged -= ValueChanged;
            }
        }

        void ValueChanged(object sender, EventArgs e) => OnPropertyChanged(nameof(Value));

        static readonly object[] emptyArray = Array.Empty<object>();
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ActiveExpression expression, MemberInfo member, ActiveExpressionOptions? options), ActiveMemberExpression> instanceInstances = new Dictionary<(ActiveExpression expression, MemberInfo member, ActiveExpressionOptions? options), ActiveMemberExpression>();
        static readonly Dictionary<(MemberInfo member, ActiveExpressionOptions? options), ActiveMemberExpression> staticInstances = new Dictionary<(MemberInfo member, ActiveExpressionOptions? options), ActiveMemberExpression>();

        public static ActiveMemberExpression Create(MemberExpression memberExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            if (memberExpression.Expression is null)
            {
                var member = memberExpression.Member;
                var key = (member, options);
                lock (instanceManagementLock)
                {
                    if (!staticInstances.TryGetValue(key, out var activeMemberExpression))
                    {
                        activeMemberExpression = new ActiveMemberExpression(memberExpression.Type, member, options, deferEvaluation);
                        staticInstances.Add(key, activeMemberExpression);
                    }
                    ++activeMemberExpression.disposalCount;
                    return activeMemberExpression;
                }
            }
            else
            {
                var expression = Create(memberExpression.Expression, options, deferEvaluation);
                var member = memberExpression.Member;
                var key = (expression, member, options);
                lock (instanceManagementLock)
                {
                    if (!instanceInstances.TryGetValue(key, out var activeMemberExpression))
                    {
                        activeMemberExpression = new ActiveMemberExpression(memberExpression.Type, expression, member, options, deferEvaluation);
                        instanceInstances.Add(key, activeMemberExpression);
                    }
                    ++activeMemberExpression.disposalCount;
                    return activeMemberExpression;
                }
            }
        }

        public static bool operator ==(ActiveMemberExpression a, ActiveMemberExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveMemberExpression a, ActiveMemberExpression b) => !(a == b);
    }
}
