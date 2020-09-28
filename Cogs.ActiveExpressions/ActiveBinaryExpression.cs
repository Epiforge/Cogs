using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Cogs.ActiveExpressions
{
    class ActiveBinaryExpression : ActiveExpression, IEquatable<ActiveBinaryExpression>
    {
        protected ActiveBinaryExpression(BinaryExpression binaryExpression, ActiveExpressionOptions? options, bool deferEvaluation, bool getDelegate = true, bool evaluateIfNotDeferred = true) : base(binaryExpression.Type, binaryExpression.NodeType, options, deferEvaluation)
        {
            try
            {
                this.binaryExpression = binaryExpression;
                left = Create(this.binaryExpression.Left, options, deferEvaluation);
                left.PropertyChanged += LeftPropertyChanged;
                switch (this.binaryExpression.NodeType)
                {
                    case ExpressionType.AndAlso when this.binaryExpression.Type == typeof(bool):
                    case ExpressionType.Coalesce:
                    case ExpressionType.OrElse when this.binaryExpression.Type == typeof(bool):
                        right = Create(this.binaryExpression.Right, options, true);
                        break;
                    default:
                        right = Create(this.binaryExpression.Right, options, deferEvaluation);
                        break;
                }
                right.PropertyChanged += RightPropertyChanged;
                isLiftedToNull = this.binaryExpression.IsLiftedToNull;
                method = this.binaryExpression.Method;
                if (getDelegate)
                {
                    var implementationKey = (NodeType, left.Type, right.Type, Type, method);
                    if (!implementations.TryGetValue(implementationKey, out var @delegate))
                    {
                        var leftParameter = Expression.Parameter(typeof(object));
                        var rightParameter = Expression.Parameter(typeof(object));
                        var leftConversion = Expression.Convert(leftParameter, left.Type);
                        var rightConversion = Expression.Convert(rightParameter, right.Type);
                        @delegate = Expression.Lambda<BinaryOperationDelegate>(Expression.Convert(method is null ? Expression.MakeBinary(NodeType, leftConversion, rightConversion) : Expression.MakeBinary(NodeType, leftConversion, rightConversion, isLiftedToNull, method), typeof(object)), leftParameter, rightParameter).Compile();
                        implementations.Add(implementationKey, @delegate);
                    }
                    this.@delegate = @delegate;
                }
                if (evaluateIfNotDeferred)
                    EvaluateIfNotDeferred();
            }
            catch (Exception ex)
            {
                DisposeValueIfNecessaryAndPossible();
                if (left is { })
                {
                    left.PropertyChanged -= LeftPropertyChanged;
                    left.Dispose();
                }
                if (right is { })
                {
                    right.PropertyChanged -= RightPropertyChanged;
                    right.Dispose();
                }
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }

        readonly BinaryExpression binaryExpression;
        readonly BinaryOperationDelegate? @delegate;
        int disposalCount;
        readonly bool isLiftedToNull;
        protected readonly ActiveExpression left;
        readonly MethodInfo? method;
        protected readonly ActiveExpression right;

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
                if (--disposalCount == 0)
                {
                    instances.Remove((binaryExpression, options));
                    result = true;
                }
            if (result)
            {
                DisposeValueIfNecessaryAndPossible();
                left.PropertyChanged -= LeftPropertyChanged;
                left.Dispose();
                right.PropertyChanged -= RightPropertyChanged;
                right.Dispose();
            }
            return result;
        }

        public override bool Equals(object? obj) => obj is ActiveBinaryExpression other && Equals(other);

        public bool Equals(ActiveBinaryExpression other) => left == other.left && method == other.method && NodeType == other.NodeType && right == other.right && Equals(options, other.options);

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            var leftValue = left.Value;
            var rightFault = right.Fault;
            var rightValue = right.Value;
            try
            {
                if (leftFault is { })
                    Fault = leftFault;
                else if (rightFault is { })
                    Fault = rightFault;
                else
                    Value = @delegate?.Invoke(leftValue, rightValue);
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveBinaryExpression), left, method, NodeType, right, options);

        protected override bool GetShouldValueBeDisposed() => method is { } && ApplicableOptions.IsMethodReturnValueDisposed(method);

        void LeftPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        void RightPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        public override string ToString() => $"{GetOperatorExpressionSyntax(NodeType, Type, left, right)} {ToStringSuffix}";

        static readonly Dictionary<(ExpressionType nodeType, Type leftType, Type rightType, Type returnValueType, MethodInfo? method), BinaryOperationDelegate> implementations = new Dictionary<(ExpressionType nodeType, Type leftType, Type rightType, Type returnValueType, MethodInfo? method), BinaryOperationDelegate>();
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(BinaryExpression binaryExpression, ActiveExpressionOptions? options), ActiveBinaryExpression> instances = new Dictionary<(BinaryExpression binaryExpression, ActiveExpressionOptions? options), ActiveBinaryExpression>(new CachedInstancesKeyComparer<BinaryExpression>());

        public static ActiveBinaryExpression Create(BinaryExpression binaryExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var key = (binaryExpression, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeBinaryExpression))
                {
                    activeBinaryExpression = binaryExpression.NodeType switch
                    {
                        ExpressionType.AndAlso when binaryExpression.Type == typeof(bool) => new ActiveAndAlsoExpression(binaryExpression, options, deferEvaluation),
                        ExpressionType.Coalesce => new ActiveCoalesceExpression(binaryExpression, options, deferEvaluation),
                        ExpressionType.OrElse when binaryExpression.Type == typeof(bool) => new ActiveOrElseExpression(binaryExpression, options, deferEvaluation),
                        _ => new ActiveBinaryExpression(binaryExpression, options, deferEvaluation),
                    };
                    instances.Add(key, activeBinaryExpression);
                }
                ++activeBinaryExpression.disposalCount;
                return activeBinaryExpression;
            }
        }

        public static bool operator ==(ActiveBinaryExpression a, ActiveBinaryExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveBinaryExpression a, ActiveBinaryExpression b) => !(a == b);
    }
}
