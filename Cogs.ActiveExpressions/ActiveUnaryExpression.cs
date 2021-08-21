namespace Cogs.ActiveExpressions;

class ActiveUnaryExpression : ActiveExpression, IEquatable<ActiveUnaryExpression>
{
    ActiveUnaryExpression(CachedInstancesKey<UnaryExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) : base(instancesKey.Expression, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    UnaryOperationDelegate? @delegate;
    int disposalCount;
    readonly CachedInstancesKey<UnaryExpression> instancesKey;
    MethodInfo? method;
    ActiveExpression? operand;

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
            if (operand is not null)
            {
                operand.PropertyChanged -= OperandPropertyChanged;
                operand.Dispose();
            }
        }
        return result;
    }

    public override bool Equals(object? obj) => obj is ActiveUnaryExpression other && Equals(other);

    public bool Equals(ActiveUnaryExpression other) => method == other.method && NodeType == other.NodeType && operand == other.operand && Equals(options, other.options);

    protected override void Evaluate()
    {
        try
        {
            var operandFault = operand?.Fault;
            if (operandFault is not null)
                Fault = operandFault;
            else
                Value = @delegate?.Invoke(operand?.Value);
        }
        catch (Exception ex)
        {
            Fault = ex;
        }
    }

    public override int GetHashCode() => HashCode.Combine(typeof(ActiveUnaryExpression), method, NodeType, operand, options);

    protected override bool GetShouldValueBeDisposed() => method is not null && ApplicableOptions.IsMethodReturnValueDisposed(method);

    protected override void Initialize()
    {
        try
        {
            operand = Create(instancesKey.Expression.Operand, options, IsDeferringEvaluation);
            operand.PropertyChanged += OperandPropertyChanged;
            method = instancesKey.Expression.Method;
            var implementationKey = new ImplementationsKey(NodeType, operand.Type, Type, method);
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
            if (operand is not null)
            {
                operand.PropertyChanged -= OperandPropertyChanged;
                operand.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    void OperandPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

    public override string ToString() => $"{GetOperatorExpressionSyntax(NodeType, Type, operand)} {ToStringSuffix}";

    static readonly Dictionary<ImplementationsKey, UnaryOperationDelegate> implementations = new Dictionary<ImplementationsKey, UnaryOperationDelegate>();
    static readonly object instanceManagementLock = new object();
    static readonly Dictionary<CachedInstancesKey<UnaryExpression>, ActiveUnaryExpression> instances = new Dictionary<CachedInstancesKey<UnaryExpression>, ActiveUnaryExpression>(new CachedInstancesKeyComparer<UnaryExpression>());

    public static ActiveUnaryExpression Create(UnaryExpression unaryExpression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        var key = new CachedInstancesKey<UnaryExpression>(unaryExpression, options);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var activeUnaryExpression))
            {
                activeUnaryExpression = new ActiveUnaryExpression(key, options, deferEvaluation);
                instances.Add(key, activeUnaryExpression);
            }
            ++activeUnaryExpression.disposalCount;
            return activeUnaryExpression;
        }
    }

    public static bool operator ==(ActiveUnaryExpression a, ActiveUnaryExpression b) => a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveUnaryExpression a, ActiveUnaryExpression b) => !(a == b);

    record ImplementationsKey(ExpressionType NodeType, Type OperandType, Type ReturnValueType, MethodInfo? Method);
}
