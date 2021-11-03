namespace Cogs.ActiveExpressions;

class ActiveInvocationExpression : ActiveExpression, IEquatable<ActiveInvocationExpression>
{
    ActiveInvocationExpression(CachedInstancesKey<InvocationExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) : base(instancesKey.Expression.Type, ExpressionType.Invoke, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    ActiveExpression? activeExpression;
    ActiveExpression? activeDelegateExpression;
    IReadOnlyList<ActiveExpression>? activeArguments;
    int disposalCount;
    readonly CachedInstancesKey<InvocationExpression> instancesKey;

    void ActiveArgumentPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (activeExpression is not null)
        {
            activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
            activeExpression.Dispose();
            activeExpression = null;
        }
        if (activeArguments.All(activeArgument => activeArgument.Fault is null))
            CreateActiveExpression();
        else if (!IsDeferringEvaluation)
            Evaluate();
    }

    void ActiveDelegateExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (activeExpression is not null)
        {
            activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
            activeExpression.Dispose();
            activeExpression = null;
        }
        CreateActiveExpression();
    }

    void ActiveExpressionPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

    void CreateActiveExpression()
    {
        switch (instancesKey.Expression.Expression)
        {
            case LambdaExpression lambdaExpression when activeArguments is not null:
                activeExpression = Create(ReplaceParameters(lambdaExpression, activeArguments.Select(activeArgument => activeArgument.Value).ToArray()), options, IsDeferringEvaluation);
                break;
            case Expression expression when typeof(Delegate).IsAssignableFrom(expression.Type):
                var activeDelegateExpressionCreated = false;
                if (activeDelegateExpression is null)
                {
                    activeDelegateExpression = Create(expression, options, IsDeferringEvaluation);
                    activeDelegateExpressionCreated = true;
                }
                if (activeDelegateExpression.Value is Delegate @delegate)
                    activeExpression = Create(@delegate.Target is null ? Expression.Call(@delegate.Method, instancesKey.Expression.Arguments) : Expression.Call(Expression.Constant(@delegate.Target), @delegate.Method, instancesKey.Expression.Arguments), options, IsDeferringEvaluation);
                if (activeDelegateExpressionCreated)
                    activeDelegateExpression.PropertyChanged += ActiveDelegateExpressionPropertyChanged;
                break;
            default:
                throw new NotSupportedException();
        }
        if (activeExpression is not null)
            activeExpression.PropertyChanged += ActiveExpressionPropertyChanged;
        EvaluateIfNotDeferred();
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
            if (activeExpression is not null)
            {
                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                activeExpression.Dispose();
            }
            if (activeDelegateExpression is not null)
            {
                activeDelegateExpression.PropertyChanged -= ActiveDelegateExpressionPropertyChanged;
                activeDelegateExpression.Dispose();
            }
            if (activeArguments is not null)
                foreach (var activeArgument in activeArguments)
                {
                    activeArgument.PropertyChanged -= ActiveArgumentPropertyChanged;
                    activeArgument.Dispose();
                }
        }
        return result;
    }

    public override bool Equals(object? obj) => obj is ActiveInvocationExpression other && Equals(other);

    public bool Equals(ActiveInvocationExpression other) => ExpressionEqualityComparer.Default.Equals(instancesKey.Expression, other.instancesKey.Expression) && Equals(options, other.options);

    protected override void Evaluate()
    {
        if (activeExpression is not null && activeExpression.Fault is { } activeExpressionFault)
            Fault = activeExpressionFault;
        else if (activeArguments is not null && activeArguments.Select(activeArgument => activeArgument.Fault).Where(fault => fault is not null).FirstOrDefault() is { } activeArgumentFault)
            Fault = activeArgumentFault;
        else if (activeExpression is not null)
            Value = activeExpression.Value;
    }

    public override int GetHashCode() => HashCode.Combine(typeof(ActiveInvocationExpression), ExpressionEqualityComparer.Default.GetHashCode(instancesKey.Expression), options);

    protected override void Initialize()
    {
        var activeArgumentsList = new List<ActiveExpression>();
        try
        {
            if (instancesKey.Expression.Expression is LambdaExpression)
            {
                foreach (var invocationExpressionArgument in instancesKey.Expression.Arguments)
                {
                    var activeArgument = Create(invocationExpressionArgument, options, IsDeferringEvaluation);
                    activeArgument.PropertyChanged += ActiveArgumentPropertyChanged;
                    activeArgumentsList.Add(activeArgument);
                }
                activeArguments = activeArgumentsList.ToImmutableArray();
            }
            CreateActiveExpression();
        }
        catch (Exception ex)
        {
            if (activeExpression is not null)
            {
                activeExpression.PropertyChanged -= ActiveExpressionPropertyChanged;
                activeExpression.Dispose();
            }
            if (activeDelegateExpression is not null)
            {
                activeDelegateExpression.PropertyChanged -= ActiveDelegateExpressionPropertyChanged;
                activeDelegateExpression.Dispose();
            }
            foreach (var activeArgument in activeArgumentsList)
            {
                activeArgument.PropertyChanged -= ActiveArgumentPropertyChanged;
                activeArgument.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public override string ToString() => $"λ({(activeExpression is not null ? (object)activeExpression : instancesKey.Expression)})";

    static readonly object instanceManagementLock = new();
    static readonly Dictionary<CachedInstancesKey<InvocationExpression>, ActiveInvocationExpression> instances = new(new CachedInstancesKeyComparer<InvocationExpression>());

    public static ActiveInvocationExpression Create(InvocationExpression invocationExpression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        var key = new CachedInstancesKey<InvocationExpression>(invocationExpression, options);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var activeInvocationExpression))
            {
                activeInvocationExpression = new ActiveInvocationExpression(key, options, deferEvaluation);
                instances.Add(key, activeInvocationExpression);
            }
            ++activeInvocationExpression.disposalCount;
            return activeInvocationExpression;
        }
    }

    public static bool operator ==(ActiveInvocationExpression a, ActiveInvocationExpression b) => a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveInvocationExpression a, ActiveInvocationExpression b) => !(a == b);
}
