namespace Cogs.ActiveExpressions;

class ActiveUnaryExpression :
    ActiveExpression,
    IEquatable<ActiveUnaryExpression>
{
    ActiveUnaryExpression(CachedInstancesKey<UnaryExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) :
        base(instancesKey.Expression, options, deferEvaluation) =>
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

    public override bool Equals(object? obj) =>
        obj is ActiveUnaryExpression other && Equals(other);

    public bool Equals(ActiveUnaryExpression other) =>
        method == other.method && NodeType == other.NodeType && operand == other.operand && Equals(options, other.options);

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

    public override int GetHashCode() =>
        HashCode.Combine(typeof(ActiveUnaryExpression), method, NodeType, operand, options);

    protected override bool GetShouldValueBeDisposed() =>
        method is not null && ApplicableOptions.IsMethodReturnValueDisposed(method);

    protected override void Initialize()
    {
        try
        {
            operand = Create(instancesKey.Expression.Operand, options, IsDeferringEvaluation);
            operand.PropertyChanged += OperandPropertyChanged;
            method = instancesKey.Expression.Method;
            @delegate = implementations.GetOrAdd(new ImplementationsKey(NodeType, operand.Type, Type, method), ImplementationsValueFactory).Value;
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

    void OperandPropertyChanged(object sender, PropertyChangedEventArgs e) =>
        Evaluate();

    public override string ToString() =>
        $"{GetOperatorExpressionSyntax(NodeType, Type, operand)} {ToStringSuffix}";

    static readonly ConcurrentDictionary<ImplementationsKey, Lazy<UnaryOperationDelegate>> implementations = new();
    static readonly object instanceManagementLock = new();
    static readonly Dictionary<CachedInstancesKey<UnaryExpression>, ActiveUnaryExpression> instances = new(new CachedInstancesKeyComparer<UnaryExpression>());

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

    static Lazy<UnaryOperationDelegate> ImplementationsValueFactory(ImplementationsKey key) =>
        new(() =>
        {
            var operandParameter = Expression.Parameter(typeof(object));
            var operandConversion = Expression.Convert(operandParameter, key.OperandType);
            return Expression.Lambda<UnaryOperationDelegate>(Expression.Convert(key.Method is null ? Expression.MakeUnary(key.NodeType, operandConversion, key.ReturnValueType) : Expression.MakeUnary(key.NodeType, operandConversion, key.ReturnValueType, key.Method), typeof(object)), operandParameter).Compile();
        }, LazyThreadSafetyMode.ExecutionAndPublication);

    public static bool operator ==(ActiveUnaryExpression a, ActiveUnaryExpression b) =>
        a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveUnaryExpression a, ActiveUnaryExpression b) =>
        !(a == b);

    record ImplementationsKey(ExpressionType NodeType, Type OperandType, Type ReturnValueType, MethodInfo? Method);
}
