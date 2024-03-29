namespace Cogs.ActiveExpressions;

class ActiveInvocationExpression :
    ActiveExpression,
    IEquatable<ActiveInvocationExpression>,
    IObserveActiveExpressions<object?>
{
    ActiveInvocationExpression(CachedInstancesKey<InvocationExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) :
        base(instancesKey.Expression.Type, ExpressionType.Invoke, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    ActiveExpression? activeExpression;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    ActiveExpression? activeDelegateExpression;
    IReadOnlyList<ActiveExpression>? activeArguments;
    int disposalCount;
    int? hashCode;
    readonly CachedInstancesKey<InvocationExpression> instancesKey;

    void IObserveActiveExpressions<object?>.ActiveExpressionChanged(IObservableActiveExpression<object?> activeExpression, object? oldValue, object? newValue, Exception? oldFault, Exception? newFault)
    {
        if (ReferenceEquals(activeExpression, this.activeExpression))
            Evaluate();
        else if (ReferenceEquals(activeExpression, activeDelegateExpression))
        {
            if (this.activeExpression is not null)
            {
                this.activeExpression.RemoveActiveExpressionObserver(this);
                this.activeExpression.Dispose();
                this.activeExpression = null;
            }
            CreateActiveExpression();
        }
        else if (activeArguments?.Contains(activeExpression) ?? false)
        {
            if (this.activeExpression is not null)
            {
                this.activeExpression.RemoveActiveExpressionObserver(this);
                this.activeExpression.Dispose();
                this.activeExpression = null;
            }
            if (activeArguments.All(activeArgument => activeArgument.Fault is null))
                CreateActiveExpression();
            else if (!IsDeferringEvaluation)
                Evaluate();
        }
    }

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
                    activeDelegateExpression.AddActiveExpressionOserver(this);
                break;
            default:
                throw new NotSupportedException();
        }
        activeExpression?.AddActiveExpressionOserver(this);
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
                activeExpression.RemoveActiveExpressionObserver(this);
                activeExpression.Dispose();
            }
            if (activeDelegateExpression is not null)
            {
                activeDelegateExpression.RemoveActiveExpressionObserver(this);
                activeDelegateExpression.Dispose();
            }
            if (activeArguments is not null)
                for (int i = 0, ii = activeArguments.Count; i < ii; ++i)
                {
                    var activeArgument = activeArguments[i];
                    activeArgument.RemoveActiveExpressionObserver(this);
                    activeArgument.Dispose();
                }
        }
        return result;
    }

    public override bool Equals(object? obj) =>
        obj is ActiveInvocationExpression other && Equals(other);

    public bool Equals(ActiveInvocationExpression other) =>
        ExpressionEqualityComparer.Default.Equals(instancesKey.Expression, other.instancesKey.Expression) && Equals(options, other.options);

    protected override void Evaluate()
    {
        if (activeExpression is not null && activeExpression.Fault is { } activeExpressionFault)
            Fault = activeExpressionFault;
        else if (activeArguments is not null && activeArguments.Select(activeArgument => activeArgument.Fault).Where(fault => fault is not null).FirstOrDefault() is { } activeArgumentFault)
            Fault = activeArgumentFault;
        else if (activeExpression is not null)
            Value = activeExpression.Value;
    }

    public override int GetHashCode() =>
        hashCode ??= HashCode.Combine(typeof(ActiveInvocationExpression), ExpressionEqualityComparer.Default.GetHashCode(instancesKey.Expression), options);

    protected override void Initialize()
    {
        var activeArgumentsList = new List<ActiveExpression>();
        try
        {
            if (instancesKey.Expression.Expression is LambdaExpression)
            {
                var invocationExpressionArguments = instancesKey.Expression.Arguments;
                for (int i = 0, ii = invocationExpressionArguments.Count; i < ii; ++i)
                {
                    var invocationExpressionArgument = invocationExpressionArguments[i];
                    var activeArgument = Create(invocationExpressionArgument, options, IsDeferringEvaluation);
                    activeArgument.AddActiveExpressionOserver(this);
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
                activeExpression.RemoveActiveExpressionObserver(this);
                activeExpression.Dispose();
            }
            if (activeDelegateExpression is not null)
            {
                activeDelegateExpression.RemoveActiveExpressionObserver(this);
                activeDelegateExpression.Dispose();
            }
            for (int i = 0, ii = activeArgumentsList.Count; i < ii; ++i)
            {
                var activeArgument = activeArgumentsList[i];
                activeArgument.RemoveActiveExpressionObserver(this);
                activeArgument.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public override string ToString() =>
        $"λ({(activeExpression is not null ? (object)activeExpression : instancesKey.Expression)})";

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

    public static bool operator ==(ActiveInvocationExpression a, ActiveInvocationExpression b) =>
        a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveInvocationExpression a, ActiveInvocationExpression b) =>
        !(a == b);
}
