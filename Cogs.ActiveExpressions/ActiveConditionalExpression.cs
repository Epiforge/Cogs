namespace Cogs.ActiveExpressions;

class ActiveConditionalExpression :
    ActiveExpression,
    IEquatable<ActiveConditionalExpression>
{
    ActiveConditionalExpression(CachedInstancesKey<ConditionalExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) :
        base(instancesKey.Expression, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    int disposalCount;
    ActiveExpression? ifFalse;
    ActiveExpression? ifTrue;
    readonly CachedInstancesKey<ConditionalExpression> instancesKey;
    ActiveExpression? test;

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
                test.PropertyChanged -= TestPropertyChanged;
                test.Dispose();
            }
            if (ifTrue is not null)
            {
                ifTrue.PropertyChanged -= IfTruePropertyChanged;
                ifTrue.Dispose();
            }
            if (ifFalse is not null)
            {
                ifFalse.PropertyChanged -= IfFalsePropertyChanged;
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
        HashCode.Combine(typeof(ActiveConditionalExpression), ifFalse, ifTrue, test, options);

    void IfFalsePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (test?.Fault is null && !(test?.Value is bool testBool && testBool))
        {
            if (e.PropertyName == nameof(Fault))
                Fault = ifFalse?.Fault;
            else if (e.PropertyName == nameof(Value) && ifFalse?.Fault is null)
                Value = ifFalse?.Value;
        }
    }

    void IfTruePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (test?.Fault is null && test?.Value is bool testBool && testBool)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = ifTrue?.Fault;
            else if (e.PropertyName == nameof(Value) && ifTrue?.Fault is null)
                Value = ifTrue?.Value;
        }
    }

    protected override void Initialize()
    {
        try
        {
            test = Create(instancesKey.Expression.Test, options, IsDeferringEvaluation);
            test.PropertyChanged += TestPropertyChanged;
            ifTrue = Create(instancesKey.Expression.IfTrue, options, true);
            ifTrue.PropertyChanged += IfTruePropertyChanged;
            ifFalse = Create(instancesKey.Expression.IfFalse, options, true);
            ifFalse.PropertyChanged += IfFalsePropertyChanged;
            EvaluateIfNotDeferred();
        }
        catch (Exception ex)
        {
            if (test is not null)
            {
                test.PropertyChanged -= TestPropertyChanged;
                test.Dispose();
            }
            if (ifTrue is not null)
            {
                ifTrue.PropertyChanged -= IfTruePropertyChanged;
                ifTrue.Dispose();
            }
            if (ifFalse is not null)
            {
                ifFalse.PropertyChanged -= IfFalsePropertyChanged;
                ifFalse.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    void TestPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Fault))
            Fault = test?.Fault;
        else if (e.PropertyName == nameof(Value) && test?.Fault is null)
        {
            if (test?.Value is bool testBool && testBool)
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
