using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Cogs.ActiveExpressions
{
    class ActiveUnaryExpression : ActiveExpression, IEquatable<ActiveUnaryExpression>
    {
        ActiveUnaryExpression(UnaryExpression unaryExpression, ActiveExpressionOptions? options, bool deferEvaluation) : base(unaryExpression.Type, unaryExpression.NodeType, options, deferEvaluation)
        {
            try
            {
                this.unaryExpression = unaryExpression;
                operand = Create(unaryExpression.Operand, options, deferEvaluation);
                operand.PropertyChanged += OperandPropertyChanged;
                method = unaryExpression.Method;
                var implementationKey = (NodeType, operand.Type, Type, method);
                if (!implementations.TryGetValue(implementationKey, out var @delegate))
                {
                    var operandParameter = Expression.Parameter(typeof(object));
                    var operandConversion = Expression.Convert(operandParameter, operand.Type);
                    @delegate = Expression.Lambda<UnaryOperationDelegate>(Expression.Convert(method is null ? Expression.MakeUnary(NodeType, operandConversion, Type) : Expression.MakeUnary(NodeType, operandConversion, Type, method), typeof(object)), operandParameter).Compile();
                    implementations.Add(implementationKey, @delegate);
                }
                this.@delegate = @delegate;
                EvaluateIfNotDeferred();
            }
            catch (Exception ex)
            {
                DisposeValueIfNecessaryAndPossible();
                if (operand is { })
                {
                    operand.PropertyChanged -= OperandPropertyChanged;
                    operand.Dispose();
                }
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        readonly UnaryOperationDelegate @delegate;
        int disposalCount;
        readonly MethodInfo? method;
        readonly ActiveExpression operand;
        readonly UnaryExpression unaryExpression;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((unaryExpression, options));
                    result = true;
                }
            if (result)
            {
                DisposeValueIfNecessaryAndPossible();
                operand.PropertyChanged -= OperandPropertyChanged;
                operand.Dispose();
            }
            return result;
        }

        public override bool Equals(object? obj) => obj is ActiveUnaryExpression other && Equals(other);

        public bool Equals(ActiveUnaryExpression other) => method == other.method && NodeType == other.NodeType && operand == other.operand && Equals(options, other.options);

        protected override void Evaluate()
        {
            try
            {
                var operandFault = operand.Fault;
                if (operandFault is { })
                    Fault = operandFault;
                else
                    Value = @delegate.Invoke(operand.Value);
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveUnaryExpression), method, NodeType, operand, options);

        protected override bool GetShouldValueBeDisposed() => method is { } && ApplicableOptions.IsMethodReturnValueDisposed(method);

        void OperandPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        public override string ToString() => $"{GetOperatorExpressionSyntax(NodeType, Type, operand)} {ToStringSuffix}";

        static readonly Dictionary<(ExpressionType nodeType, Type operandType, Type returnValueType, MethodInfo? method), UnaryOperationDelegate> implementations = new Dictionary<(ExpressionType nodeType, Type operandType, Type returnValueType, MethodInfo? method), UnaryOperationDelegate>();
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(UnaryExpression unaryExpression, ActiveExpressionOptions? options), ActiveUnaryExpression> instances = new Dictionary<(UnaryExpression unaryExpression, ActiveExpressionOptions? options), ActiveUnaryExpression>(new CachedInstancesKeyComparer<UnaryExpression>());

        public static ActiveUnaryExpression Create(UnaryExpression unaryExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var key = (unaryExpression, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeUnaryExpression))
                {
                    activeUnaryExpression = new ActiveUnaryExpression(unaryExpression, options, deferEvaluation);
                    instances.Add(key, activeUnaryExpression);
                }
                ++activeUnaryExpression.disposalCount;
                return activeUnaryExpression;
            }
        }

        public static bool operator ==(ActiveUnaryExpression a, ActiveUnaryExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveUnaryExpression a, ActiveUnaryExpression b) => !(a == b);
    }
}
