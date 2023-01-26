namespace Cogs.ActiveExpressions;

sealed class ActiveNewExpression :
    ActiveExpression,
    IEquatable<ActiveNewExpression>,
    IObserveActiveExpressions<object?>
{
    ActiveNewExpression(CachedInstancesKey<NewExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) :
        base(instancesKey.Expression, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    EquatableList<ActiveExpression> arguments;
    EquatableList<Type> constructorParameterTypes;
    int disposalCount;
    FastConstructorInfo? fastConstructor;
    readonly CachedInstancesKey<NewExpression> instancesKey;

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
        if (result)
        {
            DisposeValueIfNecessaryAndPossible();
            for (int i = 0, ii = arguments.Count; i < ii; ++i)
            {
                var argument = arguments[i];
                argument.RemoveActiveExpressionObserver(this);
                argument.Dispose();
            }
        }
        return result;
    }

    public override bool Equals(object? obj) =>
        obj is ActiveNewExpression other && Equals(other);

    public bool Equals(ActiveNewExpression other) =>
        Type == other.Type && arguments == other.arguments && Equals(options, other.options);

    protected override void Evaluate()
    {
        try
        {
            var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault is not null).FirstOrDefault();
            if (argumentFault is not null)
                Fault = argumentFault;
            else
                Value = fastConstructor is not null ? fastConstructor.Invoke(arguments.Select(argument => argument.Value).ToArray()) : Activator.CreateInstance(Type, arguments.Select(argument => argument.Value).ToArray());
        }
        catch (Exception ex)
        {
            Fault = ex;
        }
    }

    public override int GetHashCode() =>
        HashCode.Combine(typeof(ActiveNewExpression), Type, arguments, options);

    protected override bool GetShouldValueBeDisposed() =>
        ApplicableOptions.IsConstructedTypeDisposed(Type, constructorParameterTypes);

    protected override void Initialize()
    {
        var argumentsList = new List<ActiveExpression>();
        try
        {
            if (instancesKey.Expression.Constructor is { } constructor)
                fastConstructor = FastConstructorInfo.Get(constructor);
            var newExpressionArguments = instancesKey.Expression.Arguments;
            for (int i = 0, ii = newExpressionArguments.Count; i < ii; ++i)
            {
                var newExpressionArgument = newExpressionArguments[i];
                var argument = Create(newExpressionArgument, options, IsDeferringEvaluation);
                argument.AddActiveExpressionOserver(this);
                argumentsList.Add(argument);
            }
            arguments = new EquatableList<ActiveExpression>(argumentsList);
            constructorParameterTypes = new EquatableList<Type>(arguments.Select(argument => argument.Type).ToList());
            EvaluateIfNotDeferred();
        }
        catch (Exception ex)
        {
            DisposeValueIfNecessaryAndPossible();
            for (int i = 0, ii = argumentsList.Count; i < ii; ++i)
            {
                var argument = argumentsList[i];
                argument.RemoveActiveExpressionObserver(this);
                argument.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public override string ToString() =>
        $"new {Type.FullName}({string.Join(", ", arguments.Select(argument => $"{argument}"))}) {ToStringSuffix}";

    static readonly Dictionary<CachedInstancesKey<NewExpression>, ActiveNewExpression> instances = new(new CachedInstancesKeyComparer<NewExpression>());
    static readonly object instanceManagementLock = new();

    public static ActiveNewExpression Create(NewExpression newExpression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        var key = new CachedInstancesKey<NewExpression>(newExpression, options);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var activeNewExpression))
            {
                activeNewExpression = new ActiveNewExpression(key, options, deferEvaluation);
                instances.Add(key, activeNewExpression);
            }
            ++activeNewExpression.disposalCount;
            return activeNewExpression;
        }
    }

    public static bool operator ==(ActiveNewExpression a, ActiveNewExpression b) =>
        a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveNewExpression a, ActiveNewExpression b) =>
        !(a == b);
}
