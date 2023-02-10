namespace Cogs.ActiveExpressions;

sealed class ActiveMethodCallExpression :
    ActiveExpression,
    IEquatable<ActiveMethodCallExpression>,
    IObserveActiveExpressions<object?>
{
    ActiveMethodCallExpression(CachedInstancesKey<MethodCallExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) :
        base(instancesKey.Expression, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    EquatableList<ActiveExpression>? arguments;
    int disposalCount;
    FastMethodInfo? fastMethod;
    int? hashCode;
    readonly CachedInstancesKey<MethodCallExpression> instancesKey;
    MethodInfo? method;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    ActiveExpression? @object;

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
            if (@object is not null)
            {
                @object.RemoveActiveExpressionObserver(this);
                @object.Dispose();
            }
            if (arguments is { } nonNullArguments)
                for (int i = 0, ii = nonNullArguments.Count; i < ii; ++i)
                {
                    var argument = nonNullArguments[i];
                    argument.RemoveActiveExpressionObserver(this);
                    argument.Dispose();
                }
        }
        return result;
    }

    public override bool Equals(object? obj) =>
        obj is ActiveMethodCallExpression other && Equals(other);

    public bool Equals(ActiveMethodCallExpression other) =>
        arguments == other.arguments && method == other.method && Equals(@object, other.@object) && Equals(options, other.options);

    protected override void Evaluate()
    {
        try
        {
            var objectFault = @object?.Fault;
            var argumentFault = arguments?.Select(argument => argument.Fault).Where(fault => fault is not null).FirstOrDefault();
            if (objectFault is not null)
                Fault = objectFault;
            else if (argumentFault is not null)
                Fault = argumentFault;
            else
                Value = fastMethod?.Invoke(@object?.Value, arguments?.Select(argument => argument.Value).ToArray() ?? Array.Empty<object?>());
        }
        catch (Exception ex)
        {
            Fault = ex;
        }
    }

    public override int GetHashCode() =>
        hashCode ??= HashCode.Combine(typeof(ActiveMethodCallExpression), arguments, method, @object, options);

    protected override bool GetShouldValueBeDisposed() =>
        method is not null && ApplicableOptions.IsMethodReturnValueDisposed(method);

    protected override void Initialize()
    {
        var argumentsList = new List<ActiveExpression>();
        try
        {
            method = instancesKey.Expression.Method;
            fastMethod = FastMethodInfo.Get(method);
            if (instancesKey.Expression.Object is not null)
            {
                @object = Create(instancesKey.Expression.Object, options, IsDeferringEvaluation);
                @object.AddActiveExpressionOserver(this);
            }
            var methodCallExpressionArguments = instancesKey.Expression.Arguments;
            for (int i = 0, ii = methodCallExpressionArguments.Count; i < ii; ++i)
            {
                var methodCallExpressionArgument = methodCallExpressionArguments[i];
                var argument = Create(methodCallExpressionArgument, options, IsDeferringEvaluation);
                argument.AddActiveExpressionOserver(this);
                argumentsList.Add(argument);
            }
            arguments = new EquatableList<ActiveExpression>(argumentsList);
            EvaluateIfNotDeferred();
        }
        catch (Exception ex)
        {
            DisposeValueIfNecessaryAndPossible();
            if (@object is not null)
            {
                @object.RemoveActiveExpressionObserver(this);
                @object.Dispose();
            }
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
        $"{@object?.ToString() ?? method?.DeclaringType.FullName}.{method?.Name}({string.Join(", ", arguments?.Select(argument => $"{argument}"))}) {ToStringSuffix}";

    static readonly object instanceManagementLock = new();
    static readonly Dictionary<CachedInstancesKey<MethodCallExpression>, ActiveMethodCallExpression> instances = new(new CachedInstancesKeyComparer<MethodCallExpression>());

    public static ActiveMethodCallExpression Create(MethodCallExpression methodCallExpression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        var key = new CachedInstancesKey<MethodCallExpression>(methodCallExpression, options);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var activeMethodCallExpression))
            {
                activeMethodCallExpression = new ActiveMethodCallExpression(key, options, deferEvaluation);
                instances.Add(key, activeMethodCallExpression);
            }
            ++activeMethodCallExpression.disposalCount;
            return activeMethodCallExpression;
        }
    }

    public static bool operator ==(ActiveMethodCallExpression a, ActiveMethodCallExpression b) =>
        a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveMethodCallExpression a, ActiveMethodCallExpression b) =>
        !(a == b);
}
