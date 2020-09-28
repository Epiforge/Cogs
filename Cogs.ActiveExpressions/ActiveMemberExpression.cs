using Cogs.Collections;
using Cogs.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Cogs.ActiveExpressions
{
    class ActiveMemberExpression : ActiveExpression, IEquatable<ActiveMemberExpression>
    {
        ActiveMemberExpression(MemberExpression memberExpression, ActiveExpressionOptions? options, bool deferEvaluation) : base(memberExpression.Type, memberExpression.NodeType, options, deferEvaluation)
        {
            try
            {
                this.memberExpression = memberExpression;
                if (this.memberExpression.Expression is { })
                {
                    expression = Create(memberExpression.Expression, options, deferEvaluation);
                    expression.PropertyChanged += ExpressionPropertyChanged;
                }
                member = memberExpression.Member;
                switch (member)
                {
                    case FieldInfo field:
                        this.field = field;
                        isFieldOfCompilerGeneratedType = expression?.Type.Name.StartsWith("<") ?? false;
                        break;
                    case PropertyInfo property:
                        getMethod = property.GetMethod;
                        fastGetter = FastMethodInfo.Get(getMethod);
                        isFieldOfCompilerGeneratedType = false;
                        break;
                }
                EvaluateIfNotDeferred();
            }
            catch (Exception ex)
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
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        int disposalCount;
        readonly ActiveExpression? expression;
        object? expressionValue;
        readonly FastMethodInfo? fastGetter;
        readonly FieldInfo? field;
        readonly MethodInfo? getMethod;
        readonly bool isFieldOfCompilerGeneratedType;
        readonly MemberInfo member;
        readonly MemberExpression memberExpression;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((memberExpression, options));
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
            if (isFieldOfCompilerGeneratedType)
            {
                if (ApplicableOptions.MemberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged && Value is INotifyDictionaryChanged dictionaryChangedNotifier)
                    dictionaryChangedNotifier.DictionaryChanged += ValueChanged;
                else if (ApplicableOptions.MemberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged && Value is INotifyCollectionChanged collectionChangedNotifier)
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
            if (isFieldOfCompilerGeneratedType && TryGetUndeferredValue(out var value))
            {
                if (ApplicableOptions.MemberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged && value is INotifyDictionaryChanged dictionaryChangedNotifier)
                    dictionaryChangedNotifier.DictionaryChanged -= ValueChanged;
                else if (ApplicableOptions.MemberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged && value is INotifyCollectionChanged collectionChangedNotifier)
                    collectionChangedNotifier.CollectionChanged -= ValueChanged;
            }
        }

        void ValueChanged(object sender, EventArgs e) => OnPropertyChanged(nameof(Value));

        static readonly object[] emptyArray = Array.Empty<object>();
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(MemberExpression memberExpression, ActiveExpressionOptions? options), ActiveMemberExpression> instances = new Dictionary<(MemberExpression memberExpression, ActiveExpressionOptions? options), ActiveMemberExpression>(new CachedInstancesKeyComparer<MemberExpression>());

        public static ActiveMemberExpression Create(MemberExpression memberExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var key = (memberExpression, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeMemberExpression))
                {
                    activeMemberExpression = new ActiveMemberExpression(memberExpression, options, deferEvaluation);
                    instances.Add(key, activeMemberExpression);
                }
                ++activeMemberExpression.disposalCount;
                return activeMemberExpression;
            }
        }

        public static bool operator ==(ActiveMemberExpression a, ActiveMemberExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveMemberExpression a, ActiveMemberExpression b) => !(a == b);
    }
}
