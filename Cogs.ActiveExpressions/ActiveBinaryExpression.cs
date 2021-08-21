namespace Cogs.ActiveExpressions;

class ActiveBinaryExpression : ActiveExpression, IEquatable<ActiveBinaryExpression>
{
    protected ActiveBinaryExpression(CachedInstancesKey<BinaryExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation, bool getDelegate = true, bool evaluateIfNotDeferred = true) : base(instancesKey.Expression, options, deferEvaluation)
    {
        this.instancesKey = instancesKey;
        this.getDelegate = getDelegate;
        this.evaluateIfNotDeferred = evaluateIfNotDeferred;
    }

    BinaryOperationDelegate? @delegate;
    int disposalCount;
    readonly bool evaluateIfNotDeferred;
    readonly bool getDelegate;
    protected readonly CachedInstancesKey<BinaryExpression> instancesKey;
    bool isLiftedToNull;
    protected ActiveExpression? left;
    MethodInfo? method;
    protected ActiveExpression? right;

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
            if (left is not null)
            {
                left.PropertyChanged -= LeftPropertyChanged;
                left.Dispose();
            }
            if (right is not null)
            {
                right.PropertyChanged -= RightPropertyChanged;
                right.Dispose();
            }
        }
        return result;
    }

    public override bool Equals(object? obj) => obj is ActiveBinaryExpression other && Equals(other);

    public bool Equals(ActiveBinaryExpression other) => left == other.left && method == other.method && NodeType == other.NodeType && right == other.right && Equals(options, other.options);

    protected override void Evaluate()
    {
        try
        {
            var leftFault = left?.Fault;
            var leftValue = left?.Value;
            var rightFault = right?.Fault;
            var rightValue = right?.Value;
            if (leftFault is not null)
                Fault = leftFault;
            else if (rightFault is not null)
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

    protected override bool GetShouldValueBeDisposed() => method is not null && ApplicableOptions.IsMethodReturnValueDisposed(method);

    protected override void Initialize()
    {
        try
        {
            left = Create(instancesKey.Expression.Left, options, IsDeferringEvaluation);
            left.PropertyChanged += LeftPropertyChanged;
            switch (instancesKey.Expression.NodeType)
            {
                case ExpressionType.AndAlso when instancesKey.Expression.Type == typeof(bool):
                case ExpressionType.Coalesce:
                case ExpressionType.OrElse when instancesKey.Expression.Type == typeof(bool):
                    right = Create(instancesKey.Expression.Right, options, true);
                    break;
                default:
                    right = Create(instancesKey.Expression.Right, options, IsDeferringEvaluation);
                    break;
            }
            right.PropertyChanged += RightPropertyChanged;
            isLiftedToNull = instancesKey.Expression.IsLiftedToNull;
            method = instancesKey.Expression.Method;
            if (getDelegate)
            {
                var implementationKey = new ImplementationsKey(NodeType, left.Type, right.Type, Type, method);
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
            if (left is not null)
            {
                left.PropertyChanged -= LeftPropertyChanged;
                left.Dispose();
            }
            if (right is not null)
            {
                right.PropertyChanged -= RightPropertyChanged;
                right.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    void LeftPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

    void RightPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

    public override string ToString() => $"{GetOperatorExpressionSyntax(NodeType, Type, left, right)} {ToStringSuffix}";

    static readonly Dictionary<ImplementationsKey, BinaryOperationDelegate> implementations = new Dictionary<ImplementationsKey, BinaryOperationDelegate>();
    static readonly object instanceManagementLock = new object();
    static readonly Dictionary<CachedInstancesKey<BinaryExpression>, ActiveBinaryExpression> instances = new Dictionary<CachedInstancesKey<BinaryExpression>, ActiveBinaryExpression>(new CachedInstancesKeyComparer<BinaryExpression>());

    public static ActiveBinaryExpression Create(BinaryExpression binaryExpression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        var key = new CachedInstancesKey<BinaryExpression>(binaryExpression, options);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var activeBinaryExpression))
            {
                activeBinaryExpression = binaryExpression.NodeType switch
                {
                    ExpressionType.AndAlso when binaryExpression.Type == typeof(bool) => new ActiveAndAlsoExpression(key, options, deferEvaluation),
                    ExpressionType.Coalesce => new ActiveCoalesceExpression(key, options, deferEvaluation),
                    ExpressionType.OrElse when binaryExpression.Type == typeof(bool) => new ActiveOrElseExpression(key, options, deferEvaluation),
                    _ => new ActiveBinaryExpression(key, options, deferEvaluation),
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

    record ImplementationsKey(ExpressionType NodeType, Type LeftType, Type RightType, Type ReturnValueType, MethodInfo? Method);
}
