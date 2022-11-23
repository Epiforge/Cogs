namespace Cogs.ActiveExpressions;

class ActiveTypeBinaryExpression :
    ActiveExpression,
    IEquatable<ActiveTypeBinaryExpression>,
    IObserveActiveExpressions<object?>
{
    protected ActiveTypeBinaryExpression(CachedInstancesKey<TypeBinaryExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) :
        base(instancesKey.Expression, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    TypeIsDelegate? @delegate;
    int disposalCount;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    ActiveExpression? expression;
    readonly CachedInstancesKey<TypeBinaryExpression> instancesKey;
    Type? typeOperand;

    void IObserveActiveExpressions<object?>.ActiveExpressionChanged(IObservableActiveExpression<object?> activeExpression, object? oldValue, object? newValue, Exception? oldFault, Exception? newFault) =>
        Evaluate();

    protected override bool Dispose(bool disposing)
    {
        var result = false;
        lock (instanceManagementLock)
            if (--disposalCount == 0)
            {
                instances.Remove(instancesKey);
                result = true;
            }
        if (result && expression is not null)
        {
            expression.RemoveActiveExpressionObserver(this);
            expression.Dispose();
        }
        return result;
    }

    public override bool Equals(object? obj) =>
        obj is ActiveTypeBinaryExpression other && Equals(other);

    public bool Equals(ActiveTypeBinaryExpression other) =>
        expression == other.expression && typeOperand == other.typeOperand && Equals(options, other.options);

    protected override void Evaluate()
    {
        if (expression?.Fault is { } expressionFault)
            Fault = expressionFault;
        else
            Value = @delegate?.Invoke(expression?.Value);
    }

    public override int GetHashCode() =>
        HashCode.Combine(typeof(ActiveTypeBinaryExpression), expression, typeOperand, options);

    protected override void Initialize()
    {
        try
        {
            expression = Create(instancesKey.Expression.Expression, options, IsDeferringEvaluation);
            expression.AddActiveExpressionOserver(this);
            typeOperand = instancesKey.Expression.TypeOperand;
            var parameter = Expression.Parameter(typeof(object));
            @delegate = delegates.GetOrAdd(typeOperand, CreateDelegate);
            EvaluateIfNotDeferred();
        }
        catch (Exception ex)
        {
            if (expression is not null)
            {
                expression.RemoveActiveExpressionObserver(this);
                expression.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public override string ToString() =>
        $"{GetOperatorExpressionSyntax(NodeType, Type, expression, typeOperand)} {ToStringSuffix}";

    static readonly ConcurrentDictionary<Type, TypeIsDelegate> delegates = new();
    static readonly object instanceManagementLock = new();
    static readonly Dictionary<CachedInstancesKey<TypeBinaryExpression>, ActiveTypeBinaryExpression> instances = new(new CachedInstancesKeyComparer<TypeBinaryExpression>());

    public static ActiveTypeBinaryExpression Create(TypeBinaryExpression typeBinaryExpression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        var key = new CachedInstancesKey<TypeBinaryExpression>(typeBinaryExpression, options);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var activeTypeBinaryExpression))
            {
                activeTypeBinaryExpression = new ActiveTypeBinaryExpression(key, options, deferEvaluation);
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

    public static bool operator ==(ActiveTypeBinaryExpression a, ActiveTypeBinaryExpression b) =>
        a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveTypeBinaryExpression a, ActiveTypeBinaryExpression b) =>
        !(a == b);
}
