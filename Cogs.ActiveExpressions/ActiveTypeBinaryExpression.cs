using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class ActiveTypeBinaryExpression : ActiveExpression, IEquatable<ActiveTypeBinaryExpression>
    {
        protected ActiveTypeBinaryExpression(ActiveExpression expression, Type typeOperand, ActiveExpressionOptions? options, bool deferEvaluation) : base(typeof(bool), ExpressionType.TypeIs, options, deferEvaluation)
        {
            this.expression = expression;
            this.expression.PropertyChanged += ExpressionPropertyChanged;
            this.typeOperand = typeOperand;
            var parameter = Expression.Parameter(typeof(object));
            @delegate = delegates.GetOrAdd(this.typeOperand, CreateDelegate);
            EvaluateIfNotDeferred();
        }

        readonly TypeIsDelegate @delegate;
        int disposalCount;
        readonly ActiveExpression expression;
        readonly Type typeOperand;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((expression, typeOperand, options));
                    result = true;
                }
            if (result)
            {
                expression.PropertyChanged -= ExpressionPropertyChanged;
                expression.Dispose();
            }
            return result;
        }


        public override bool Equals(object obj) => obj is ActiveTypeBinaryExpression other && Equals(other);

        public bool Equals(ActiveTypeBinaryExpression other) => expression.Equals(other.expression) && typeOperand.Equals(other.typeOperand) && Equals(options, other.options);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        protected override void Evaluate()
        {
            try
            {
                if (expression.Fault is { } expressionFault)
                    Fault = expressionFault;
                else
                    Value = @delegate.Invoke(expression.Value);
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveTypeBinaryExpression), expression, typeOperand, options);

        public override string ToString() => $"{GetOperatorExpressionSyntax(NodeType, Type, expression, typeOperand)} {ToStringSuffix}";

        static readonly ConcurrentDictionary<Type, TypeIsDelegate> delegates = new ConcurrentDictionary<Type, TypeIsDelegate>();
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ActiveExpression expression, Type typeOperand, ActiveExpressionOptions? options), ActiveTypeBinaryExpression> instances = new Dictionary<(ActiveExpression expression, Type typeOperand, ActiveExpressionOptions? options), ActiveTypeBinaryExpression>();

        public static ActiveTypeBinaryExpression Create(TypeBinaryExpression typeBinaryExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var expression = Create(typeBinaryExpression.Expression, options, deferEvaluation);
            var typeOperand = typeBinaryExpression.TypeOperand;
            var key = (expression, typeOperand, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeTypeBinaryExpression))
                {
                    activeTypeBinaryExpression = new ActiveTypeBinaryExpression(expression, typeOperand, options, deferEvaluation);
                    instances.Add(key, activeTypeBinaryExpression);
                }
                ++activeTypeBinaryExpression.disposalCount;
                return activeTypeBinaryExpression;
            }
        }

        static TypeIsDelegate CreateDelegate(Type type)
        {
            var parameter = Expression.Parameter(typeof(object));
            return Expression.Lambda<TypeIsDelegate>(Expression.TypeIs(parameter, type), parameter).Compile();
        }

        public static bool operator ==(ActiveTypeBinaryExpression? a, ActiveTypeBinaryExpression? b) => EqualityComparer<ActiveTypeBinaryExpression?>.Default.Equals(a, b);

        public static bool operator !=(ActiveTypeBinaryExpression? a, ActiveTypeBinaryExpression? b) => !EqualityComparer<ActiveTypeBinaryExpression?>.Default.Equals(a, b);
    }
}
