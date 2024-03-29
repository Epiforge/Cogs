namespace Cogs.ActiveExpressions;

sealed class ActiveCoalesceExpression :
    ActiveBinaryExpression,
    IEquatable<ActiveCoalesceExpression>
{
    public ActiveCoalesceExpression(CachedInstancesKey<BinaryExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) :
        base(instancesKey, options, deferEvaluation, false, false)
    {
    }

    UnaryOperationDelegate? conversionDelegate;
    int? hashCode;

    public override bool Equals(object? obj) =>
        obj is ActiveCoalesceExpression other && Equals(other);

    public bool Equals(ActiveCoalesceExpression other) =>
        left == other.left && right == other.right && Equals(options, other.options);

    protected override void Evaluate()
    {
        try
        {
            var leftFault = left?.Fault;
            if (leftFault is not null)
                Fault = leftFault;
            else
            {
                var leftValue = left?.Value;
                if (leftValue is not null)
                    Value = conversionDelegate is null ? leftValue : conversionDelegate(leftValue);
                else
                {
                    var rightFault = right?.Fault;
                    if (rightFault is not null)
                        Fault = rightFault;
                    else
                        Value = right?.Value;
                }
            }
        }
        catch (Exception ex)
        {
            Fault = ex;
        }
    }

    public override int GetHashCode() =>
        hashCode ??= HashCode.Combine(typeof(ActiveCoalesceExpression), left, right, options);

    protected override void Initialize()
    {
        base.Initialize();
        if (instancesKey.Expression.Conversion is { } conversion)
        {
            var key = new ConversionDelegatesKey(conversion.Parameters[0].Type, conversion.Body.Type);
            lock (conversionDelegateManagementLock)
            {
                if (!conversionDelegates.TryGetValue(key, out var conversionDelegate))
                {
                    var parameter = Expression.Parameter(typeof(object));
                    conversionDelegate = Expression.Lambda<UnaryOperationDelegate>(Expression.Convert(Expression.Invoke(conversion, Expression.Convert(parameter, key.ConvertFrom)), typeof(object)), parameter).Compile();
                    conversionDelegates.Add(key, conversionDelegate);
                }
                this.conversionDelegate = conversionDelegate;
            }
        }
        EvaluateIfNotDeferred();
    }

    public override string ToString() =>
        $"({left} ?? {right}) {ToStringSuffix}";

    static readonly object conversionDelegateManagementLock = new();
    static readonly Dictionary<ConversionDelegatesKey, UnaryOperationDelegate> conversionDelegates = new();

    public static bool operator ==(ActiveCoalesceExpression a, ActiveCoalesceExpression b) =>
        a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveCoalesceExpression a, ActiveCoalesceExpression b) =>
        !(a == b);

    sealed record ConversionDelegatesKey(Type ConvertFrom, Type ConvertTo);
}
