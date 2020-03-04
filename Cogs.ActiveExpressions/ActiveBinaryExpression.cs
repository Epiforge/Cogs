using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Cogs.ActiveExpressions
{
    class ActiveBinaryExpression : ActiveExpression, IEquatable<ActiveBinaryExpression>
    {
        protected ActiveBinaryExpression(Type type, ExpressionType nodeType, ActiveExpression left, ActiveExpression right, bool isLiftedToNull, MethodInfo? method, ActiveExpressionOptions? options, bool deferEvaluation, bool getDelegate = true, bool evaluateIfNotDeferred = true) : base(type, nodeType, options, deferEvaluation)
        {
            this.left = left;
            this.left.PropertyChanged += LeftPropertyChanged;
            this.right = right;
            this.right.PropertyChanged += RightPropertyChanged;
            this.isLiftedToNull = isLiftedToNull;
            this.method = method;
            if (getDelegate)
            {
                var implementationKey = (NodeType, this.left.Type, this.right.Type, Type, this.method);
                if (!implementations.TryGetValue(implementationKey, out var @delegate))
                {
                    var leftParameter = Expression.Parameter(typeof(object));
                    var rightParameter = Expression.Parameter(typeof(object));
                    var leftConversion = Expression.Convert(leftParameter, this.left.Type);
                    var rightConversion = Expression.Convert(rightParameter, this.right.Type);
                    @delegate = Expression.Lambda<BinaryOperationDelegate>(Expression.Convert(this.method is null ? Expression.MakeBinary(NodeType, leftConversion, rightConversion) : Expression.MakeBinary(NodeType, leftConversion, rightConversion, this.isLiftedToNull, this.method), typeof(object)), leftParameter, rightParameter).Compile();
                    implementations.Add(implementationKey, @delegate);
                }
                this.@delegate = @delegate;
            }
            if (evaluateIfNotDeferred)
                EvaluateIfNotDeferred();
        }

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
                    instances.Remove((NodeType, left, right, isLiftedToNull, method, options));
                    result = true;
                }
            if (result)
            {
                left.PropertyChanged -= LeftPropertyChanged;
                left.Dispose();
                right.PropertyChanged -= RightPropertyChanged;
                right.Dispose();
                DisposeValueIfNecessary();
            }
            return result;
        }

        void DisposeValueIfNecessary()
        {
            if (method is { } && ApplicableOptions.IsMethodReturnValueDisposed(method) && TryGetUndeferredValue(out var value))
            {
                if (value is IDisposable disposable)
                    disposable.Dispose();
                else if (value is IAsyncDisposable asyncDisposable)
                    asyncDisposable.DisposeAsync().AsTask().Wait();
            }
        }

        public override bool Equals(object obj) => obj is ActiveBinaryExpression other && Equals(other);

        public bool Equals(ActiveBinaryExpression other) => left.Equals(other.left) && Equals(method, other.method) && NodeType.Equals(other.NodeType) && right.Equals(other.right) && Equals(options, other.options);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Don't tell me what to catch in a general purpose method, bruh")]
        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            var leftValue = left.Value;
            var rightFault = right.Fault;
            var rightValue = right.Value;
            try
            {
                DisposeValueIfNecessary();
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

        void LeftPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        void RightPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        public override string ToString() => $"{GetOperatorExpressionSyntax(NodeType, Type, left, right)} {ToStringSuffix}";

        static readonly Dictionary<(ExpressionType nodeType, Type leftType, Type rightType, Type returnValueType, MethodInfo? method), BinaryOperationDelegate> implementations = new Dictionary<(ExpressionType nodeType, Type leftType, Type rightType, Type returnValueType, MethodInfo? method), BinaryOperationDelegate>();
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ExpressionType nodeType, ActiveExpression left, ActiveExpression right, bool isLiftedToNull, MethodInfo? method, ActiveExpressionOptions? options), ActiveBinaryExpression> instances = new Dictionary<(ExpressionType nodeType, ActiveExpression left, ActiveExpression right, bool isLiftedToNull, MethodInfo? method, ActiveExpressionOptions? options), ActiveBinaryExpression>();

        public static ActiveBinaryExpression Create(BinaryExpression binaryExpression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            var type = binaryExpression.Type;
            var nodeType = binaryExpression.NodeType;
            var left = Create(binaryExpression.Left, options, deferEvaluation);
            var isLiftedToNull = binaryExpression.IsLiftedToNull;
            var method = binaryExpression.Method;
            ActiveExpression right;
            switch (nodeType)
            {
                case ExpressionType.AndAlso when type == typeof(bool):
                case ExpressionType.Coalesce:
                case ExpressionType.OrElse when type == typeof(bool):
                    right = Create(binaryExpression.Right, options, true);
                    break;
                default:
                    right = Create(binaryExpression.Right, options, deferEvaluation);
                    break;
            }
            var key = (nodeType, left, right, isLiftedToNull, method, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeBinaryExpression))
                {
                    activeBinaryExpression = nodeType switch
                    {
                        ExpressionType.AndAlso when type == typeof(bool) => new ActiveAndAlsoExpression(left, right, options, deferEvaluation),
                        ExpressionType.Coalesce => new ActiveCoalesceExpression(type, left, right, binaryExpression.Conversion, options, deferEvaluation),
                        ExpressionType.OrElse when type == typeof(bool) => new ActiveOrElseExpression(left, right, options, deferEvaluation),
                        _ => new ActiveBinaryExpression(type, nodeType, left, right, isLiftedToNull, method, options, deferEvaluation),
                    };
                    instances.Add(key, activeBinaryExpression);
                }
                ++activeBinaryExpression.disposalCount;
                return activeBinaryExpression;
            }
        }

        public static bool operator ==(ActiveBinaryExpression? a, ActiveBinaryExpression? b) => EqualityComparer<ActiveBinaryExpression?>.Default.Equals(a, b);

        public static bool operator !=(ActiveBinaryExpression? a, ActiveBinaryExpression? b) => !EqualityComparer<ActiveBinaryExpression?>.Default.Equals(a, b);
    }
}
