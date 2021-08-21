namespace Cogs.ActiveExpressions;

class ActiveNewExpression : ActiveExpression, IEquatable<ActiveNewExpression>
{
    ActiveNewExpression(CachedInstancesKey<NewExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) : base(instancesKey.Expression, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    protected override void Initialize()
    {
        var argumentsList = new List<ActiveExpression>();
        try
        {
            if (instancesKey.Expression.Constructor is { } constructor)
                fastConstructor = FastConstructorInfo.Get(constructor);
            foreach (var newExpressionArgument in instancesKey.Expression.Arguments)
            {
                var argument = Create(newExpressionArgument, options, IsDeferringEvaluation);
                argument.PropertyChanged += ArgumentPropertyChanged;
                argumentsList.Add(argument);
            }
            arguments = new EquatableList<ActiveExpression>(argumentsList);
            constructorParameterTypes = new EquatableList<Type>(arguments.Select(argument => argument.Type).ToList());
            EvaluateIfNotDeferred();
        }
        catch (Exception ex)
        {
            DisposeValueIfNecessaryAndPossible();
            foreach (var argument in argumentsList)
            {
                argument.PropertyChanged -= ArgumentPropertyChanged;
                argument.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    EquatableList<ActiveExpression> arguments;
    EquatableList<Type> constructorParameterTypes;
    int disposalCount;
    FastConstructorInfo? fastConstructor;
    readonly CachedInstancesKey<NewExpression> instancesKey;

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
            foreach (var argument in arguments)
            {
                argument.PropertyChanged -= ArgumentPropertyChanged;
                argument.Dispose();
            }
        }
        return result;
    }

    public override bool Equals(object? obj) => obj is ActiveNewExpression other && Equals(other);

    public bool Equals(ActiveNewExpression other) => Type == other.Type && arguments == other.arguments && Equals(options, other.options);

    protected override void Evaluate()
    {
        try
        {
            var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault is not null).FirstOrDefault();
            if (argumentFault is not null)
                Fault = argumentFault;
            else if (fastConstructor is not null)
                Value = fastConstructor.Invoke(arguments.Select(argument => argument.Value).ToArray());
            else
                Value = Activator.CreateInstance(Type, arguments.Select(argument => argument.Value).ToArray());
        }
        catch (Exception ex)
        {
            Fault = ex;
        }
    }

    public override int GetHashCode() => HashCode.Combine(typeof(ActiveNewExpression), Type, arguments, options);

    protected override bool GetShouldValueBeDisposed() => ApplicableOptions.IsConstructedTypeDisposed(Type, constructorParameterTypes);

    public override string ToString() => $"new {Type.FullName}({string.Join(", ", arguments.Select(argument => $"{argument}"))}) {ToStringSuffix}";

    static readonly Dictionary<CachedInstancesKey<NewExpression>, ActiveNewExpression> instances = new Dictionary<CachedInstancesKey<NewExpression>, ActiveNewExpression>(new CachedInstancesKeyComparer<NewExpression>());
    static readonly object instanceManagementLock = new object();

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

    public static bool operator ==(ActiveNewExpression a, ActiveNewExpression b) => a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveNewExpression a, ActiveNewExpression b) => !(a == b);
}
