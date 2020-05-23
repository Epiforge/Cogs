using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Cogs.ActiveExpressions
{
    class ActiveUnaryExpression : ActiveExpression, IEquatable<ActiveUnaryExpression>
    {
        ActiveUnaryExpression(ExpressionType nodeType, ActiveExpression operand, Type type, MethodInfo? method, ActiveExpressionOptions? options, bool deferEvaluation) : base(type, nodeType, options, deferEvaluation)
        {
            this.operand = operand;
            this.operand.PropertyChanged += OperandPropertyChanged;
            this.method = method;
            var implementationKey = (NodeType, this.operand.Type, Type, this.method);
            if (!implementations.TryGetValue(implementationKey, out var @delegate))
            {
                var operandParameter = Expression.Parameter(typeof(object));
                var operandConversion = Expression.Convert(operandParameter, this.operand.Type);
                @delegate = Expression.Lambda<UnaryOperationDelegate>(Expression.Convert(this.method is null ? Expression.MakeUnary(NodeType, operandConversion, Type) : Expression.MakeUnary(NodeType, operandConversion, Type, this.method), typeof(object)), operandParameter).Compile();
                implementations.Add(implementationKey, @delegate);
            }
            this.@delegate = @delegate;
            EvaluateIfNotDeferred();
        }

        readonly UnaryOperationDelegate @delegate;
        int disposalCount;
        readonly MethodInfo? method;
        readonly ActiveExpression operand;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((NodeType, operand, Type, method, options));
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

        public override bool Equals(object obj) => obj is ActiveUnaryExpression other && Equals(other);

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
        static readonly Dictionary<(ExpressionType nodeType, ActiveExpression opperand, Type type, MethodInfo? method, ActiveExpressionOptions? options), ActiveUnaryExpression> instances = new Dictionary<(ExpressionType nodeType, ActiveExpression opperand, Type type, MethodInfo? method, ActiveExpressionOptions? options), ActiveUnaryExpression>();

        public static ActiveUnaryExpression Create(UnaryExpression unaryExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var nodeType = unaryExpression.NodeType;
            var operand = Create(unaryExpression.Operand, options, deferEvaluation);
            var type = unaryExpression.Type;
            var method = unaryExpression.Method;
            var key = (nodeType, operand, type, method, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeUnaryExpression))
                {
                    activeUnaryExpression = new ActiveUnaryExpression(nodeType, operand, type, method, options, deferEvaluation);
                    instances.Add(key, activeUnaryExpression);
                }
                ++activeUnaryExpression.disposalCount;
                return activeUnaryExpression;
            }
        }

        public static bool operator ==(ActiveUnaryExpression a, ActiveUnaryExpression b) => a.Equals(b);

        public static bool operator !=(ActiveUnaryExpression a, ActiveUnaryExpression b) => !(a == b);
    }
}
