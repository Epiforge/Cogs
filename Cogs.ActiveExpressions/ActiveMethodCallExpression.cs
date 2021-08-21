namespace Cogs.ActiveExpressions;

class ActiveMethodCallExpression : ActiveExpression, IEquatable<ActiveMethodCallExpression>
{
    ActiveMethodCallExpression(CachedInstancesKey<MethodCallExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) : base(instancesKey.Expression, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    EquatableList<ActiveExpression>? arguments;
    int disposalCount;
    FastMethodInfo? fastMethod;
    readonly CachedInstancesKey<MethodCallExpression> instancesKey;
    MethodInfo? method;
    ActiveExpression? @object;

    void ArgumentPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

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
                @object.PropertyChanged -= ObjectPropertyChanged;
                @object.Dispose();
            }
            if (arguments is not null)
                foreach (var argument in arguments)
                {
                    argument.PropertyChanged -= ArgumentPropertyChanged;
                    argument.Dispose();
                }
        }
        return result;
    }

    public override bool Equals(object? obj) => obj is ActiveMethodCallExpression other && Equals(other);

    public bool Equals(ActiveMethodCallExpression other) => arguments == other.arguments && method == other.method && Equals(@object, other.@object) && Equals(options, other.options);

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

    public override int GetHashCode() => HashCode.Combine(typeof(ActiveMethodCallExpression), arguments, method, @object, options);

    protected override bool GetShouldValueBeDisposed() => method is not null && ApplicableOptions.IsMethodReturnValueDisposed(method);

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
                @object.PropertyChanged += ObjectPropertyChanged;
            }
            foreach (var methodCallExpressionArgument in instancesKey.Expression.Arguments)
            {
                var argument = Create(methodCallExpressionArgument, options, IsDeferringEvaluation);
                argument.PropertyChanged += ArgumentPropertyChanged;
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
                @object.PropertyChanged -= ObjectPropertyChanged;
                @object.Dispose();
            }
            foreach (var argument in argumentsList)
            {
                argument.PropertyChanged -= ArgumentPropertyChanged;
                argument.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    void ObjectPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

    public override string ToString() => $"{@object?.ToString() ?? method?.DeclaringType.FullName}.{method?.Name}({string.Join(", ", arguments?.Select(argument => $"{argument}"))}) {ToStringSuffix}";

    static readonly object instanceManagementLock = new object();
    static readonly Dictionary<CachedInstancesKey<MethodCallExpression>, ActiveMethodCallExpression> instances = new Dictionary<CachedInstancesKey<MethodCallExpression>, ActiveMethodCallExpression>(new CachedInstancesKeyComparer<MethodCallExpression>());

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

    public static bool operator ==(ActiveMethodCallExpression a, ActiveMethodCallExpression b) => a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveMethodCallExpression a, ActiveMethodCallExpression b) => !(a == b);
}
