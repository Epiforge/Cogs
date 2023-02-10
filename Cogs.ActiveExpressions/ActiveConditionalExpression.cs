namespace Cogs.ActiveExpressions;

sealed class ActiveConditionalExpression :
    ActiveExpression,
    IEquatable<ActiveConditionalExpression>,
    IObserveActiveExpressions<object?>
{
    ActiveConditionalExpression(CachedInstancesKey<ConditionalExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) :
        base(instancesKey.Expression, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    int disposalCount;
    int? hashCode;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    ActiveExpression? ifFalse;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    ActiveExpression? ifTrue;
    readonly CachedInstancesKey<ConditionalExpression> instancesKey;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    ActiveExpression? test;

    void IObserveActiveExpressions<object?>.ActiveExpressionChanged(IObservableActiveExpression<object?> activeExpression, object? oldValue, object? newValue, Exception? oldFault, Exception? newFault)
    {
        if (ReferenceEquals(activeExpression, test))
        {
            if (newFault is not null)
                Fault = newFault;
            else if (newValue is bool testValue)
            {
                if (testValue)
                {
                    if (ifTrue?.Fault is { } trueFault)
                        Fault = trueFault;
                    else
                        Value = ifTrue?.Value;
                }
                else
                {
                    if (ifFalse?.Fault is { } falseFault)
                        Fault = falseFault;
                    else
                        Value = ifFalse?.Value;
                }
            }
        }
        else if (test?.Fault is null && test?.Value is bool testValue && testValue == ReferenceEquals(activeExpression, ifTrue))
        {
            if (newFault is { } nonNullFault)
                Fault = nonNullFault;
            else
                Value = newValue;
        }
    }

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
            if (test is not null)
            {
                test.RemoveActiveExpressionObserver(this);
                test.Dispose();
            }
            if (ifTrue is not null)
            {
                ifTrue.RemoveActiveExpressionObserver(this);
                ifTrue.Dispose();
            }
            if (ifFalse is not null)
            {
                ifFalse.RemoveActiveExpressionObserver(this);
                ifFalse.Dispose();
            }
        }
        return result;
    }

    public override bool Equals(object? obj) =>
        obj is ActiveConditionalExpression other && Equals(other);

    public bool Equals(ActiveConditionalExpression other) =>
        ifFalse == other.ifFalse && ifTrue == other.ifTrue && test == other.test && Equals(options, other.options);

    protected override void Evaluate()
    {
        var testFault = test?.Fault;
        if (testFault is not null)
            Fault = testFault;
        else if (test?.Value is bool testBool && testBool)
        {
            var ifTrueFault = ifTrue?.Fault;
            if (ifTrueFault is not null)
                Fault = ifTrueFault;
            else
                Value = ifTrue?.Value;
        }
        else
        {
            var ifFalseFault = ifFalse?.Fault;
            if (ifFalseFault is not null)
                Fault = ifFalseFault;
            else
                Value = ifFalse?.Value;
        }
    }

    public override int GetHashCode() =>
        hashCode ??= HashCode.Combine(typeof(ActiveConditionalExpression), ifFalse, ifTrue, test, options);

    protected override void Initialize()
    {
        try
        {
            test = Create(instancesKey.Expression.Test, options, IsDeferringEvaluation);
            test.AddActiveExpressionOserver(this);
            ifTrue = Create(instancesKey.Expression.IfTrue, options, true);
            ifTrue.AddActiveExpressionOserver(this);
            ifFalse = Create(instancesKey.Expression.IfFalse, options, true);
            ifFalse.AddActiveExpressionOserver(this);
            EvaluateIfNotDeferred();
        }
        catch (Exception ex)
        {
            if (test is not null)
            {
                test.RemoveActiveExpressionObserver(this);
                test.Dispose();
            }
            if (ifTrue is not null)
            {
                ifTrue.RemoveActiveExpressionObserver(this);
                ifTrue.Dispose();
            }
            if (ifFalse is not null)
            {
                ifFalse.RemoveActiveExpressionObserver(this);
                ifFalse.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public override string ToString() =>
        $"({test} ? {ifTrue} : {ifFalse}) {ToStringSuffix}";

    static readonly object instanceManagementLock = new();
    static readonly Dictionary<CachedInstancesKey<ConditionalExpression>, ActiveConditionalExpression> instances = new(new CachedInstancesKeyComparer<ConditionalExpression>());

    public static ActiveConditionalExpression Create(ConditionalExpression conditionalExpression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        var key = new CachedInstancesKey<ConditionalExpression>(conditionalExpression, options);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var activeConditionalExpression))
            {
                activeConditionalExpression = new ActiveConditionalExpression(key, options, deferEvaluation);
                instances.Add(key, activeConditionalExpression);
            }
            ++activeConditionalExpression.disposalCount;
            return activeConditionalExpression;
        }
    }

    public static bool operator ==(ActiveConditionalExpression a, ActiveConditionalExpression b) =>
        a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveConditionalExpression a, ActiveConditionalExpression b) =>
        !(a == b);
}
