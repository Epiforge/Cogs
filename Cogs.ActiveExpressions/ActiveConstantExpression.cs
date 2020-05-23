using Cogs.Collections;
using Cogs.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Cogs.ActiveExpressions
{
    class ActiveConstantExpression : ActiveExpression, IEquatable<ActiveConstantExpression>
    {
        ActiveConstantExpression(Type type, object value, ActiveExpressionOptions? options) : base(type, ExpressionType.Constant, options, value)
        {
            if (ApplicableOptions.ConstantExpressionsListenForDictionaryChanged && value is INotifyDictionaryChanged dictionaryChangedNotifier)
                dictionaryChangedNotifier.DictionaryChanged += ValueChanged;
            else if (ApplicableOptions.ConstantExpressionsListenForCollectionChanged && value is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged += ValueChanged;
        }

        int disposalCount;

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                if (typeof(Expression).GetTypeInfo().IsAssignableFrom(Type.GetTypeInfo()))
                    expressionInstances.Remove(((Expression?)Value, options));
                else
                    instances.Remove((Type, Value, options));
                if (ApplicableOptions.ConstantExpressionsListenForDictionaryChanged && Value is INotifyDictionaryChanged dictionaryChangedNotifier)
                    dictionaryChangedNotifier.DictionaryChanged -= ValueChanged;
                else if (ApplicableOptions.ConstantExpressionsListenForCollectionChanged && Value is INotifyCollectionChanged collectionChangedNotifier)
                    collectionChangedNotifier.CollectionChanged -= ValueChanged;
                return true;
            }
        }

        public override bool Equals(object obj) => obj is ActiveConstantExpression other && Equals(other);

        public bool Equals(ActiveConstantExpression other) => Type == other.Type && FastEqualityComparer.Get(Type).Equals(Value, other.Value) && Equals(options, other.options);

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveConstantExpression), Value);

        public override string ToString() => $"{{C}} {ToStringSuffix}";

        void ValueChanged(object sender, EventArgs e) => OnPropertyChanged(nameof(Value));

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(Expression? expression, ActiveExpressionOptions? options), ActiveConstantExpression> expressionInstances = new Dictionary<(Expression? expression, ActiveExpressionOptions? options), ActiveConstantExpression>(ExpressionInstanceKeyComparer.Default);
        static readonly Dictionary<(Type type, object? value, ActiveExpressionOptions? options), ActiveConstantExpression> instances = new Dictionary<(Type type, object? value, ActiveExpressionOptions? options), ActiveConstantExpression>();

        public static ActiveConstantExpression Create(ConstantExpression constantExpression, ActiveExpressionOptions? options)
        {
            var type = constantExpression.Type;
            var value = constantExpression.Value;
            if (typeof(Expression).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                var key = ((Expression)value, options);
                lock (instanceManagementLock)
                {
                    if (!expressionInstances.TryGetValue(key, out var activeConstantExpression))
                    {
                        activeConstantExpression = new ActiveConstantExpression(type, value, options);
                        expressionInstances.Add(key, activeConstantExpression);
                    }
                    ++activeConstantExpression.disposalCount;
                    return activeConstantExpression;
                }
            }
            else
            {
                var key = (type, value, options);
                lock (instanceManagementLock)
                {
                    if (!instances.TryGetValue(key, out var activeConstantExpression))
                    {
                        activeConstantExpression = new ActiveConstantExpression(type, value, options);
                        instances.Add(key, activeConstantExpression);
                    }
                    ++activeConstantExpression.disposalCount;
                    return activeConstantExpression;
                }
            }
        }

        public static bool operator ==(ActiveConstantExpression a, ActiveConstantExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveConstantExpression a, ActiveConstantExpression b) => !(a == b);

        class ExpressionInstanceKeyComparer : IEqualityComparer<(Expression? expression, ActiveExpressionOptions? options)>
        {
            public static ExpressionInstanceKeyComparer Default { get; } = new ExpressionInstanceKeyComparer();

            public bool Equals((Expression? expression, ActiveExpressionOptions? options) x, (Expression? expression, ActiveExpressionOptions? options) y) => Equals(x.expression, y.expression) && Equals(x.options, y.options);

            public int GetHashCode((Expression? expression, ActiveExpressionOptions? options) obj) => HashCode.Combine(obj.expression is Expression objExpression ? ExpressionEqualityComparer.Default.GetHashCode(objExpression) : 0, obj.options?.GetHashCode() ?? 0);
        }
    }
}
