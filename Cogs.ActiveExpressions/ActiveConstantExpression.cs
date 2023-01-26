namespace Cogs.ActiveExpressions;

sealed class ActiveConstantExpression :
    ActiveExpression,
    IEquatable<ActiveConstantExpression>
{
    ActiveConstantExpression(Type type, object value, ActiveExpressionOptions? options, ExpressionInstancesKey? expressionInstancesKey, InstancesKey? instancesKey) :
        base(type, ExpressionType.Constant, options, value)
    {
        this.expressionInstancesKey = expressionInstancesKey;
        this.instancesKey = instancesKey;
        if (ApplicableOptions.ConstantExpressionsListenForDictionaryChanged && value is INotifyDictionaryChanged dictionaryChangedNotifier)
            dictionaryChangedNotifier.DictionaryChanged += ValueChanged;
        else if (ApplicableOptions.ConstantExpressionsListenForCollectionChanged && value is INotifyCollectionChanged collectionChangedNotifier)
            collectionChangedNotifier.CollectionChanged += ValueChanged;
    }

    int disposalCount;
    readonly ExpressionInstancesKey? expressionInstancesKey;
    readonly InstancesKey? instancesKey;

    protected override bool Dispose(bool disposing)
    {
        lock (instanceManagementLock)
        {
            if (--disposalCount > 0)
                return false;
            if (expressionInstancesKey is not null)
                expressionInstances.Remove(expressionInstancesKey);
            else
                instances.Remove(instancesKey!);
            if (ApplicableOptions.ConstantExpressionsListenForDictionaryChanged && Value is INotifyDictionaryChanged dictionaryChangedNotifier)
                dictionaryChangedNotifier.DictionaryChanged -= ValueChanged;
            else if (ApplicableOptions.ConstantExpressionsListenForCollectionChanged && Value is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged -= ValueChanged;
            return true;
        }
    }

    public override bool Equals(object? obj) =>
        obj is ActiveConstantExpression other && Equals(other);

    public bool Equals(ActiveConstantExpression other) =>
        Type == other.Type && FastEqualityComparer.Get(Type).Equals(Value, other.Value) && Equals(options, other.options);

    public override int GetHashCode() =>
        HashCode.Combine(typeof(ActiveConstantExpression), Value);

    protected override void Initialize()
    {
        // nothing to do here
    }

    public override string ToString() =>
        $"{{C}} {ToStringSuffix}";

    void ValueChanged(object sender, EventArgs e) =>
        OnPropertyChanged(nameof(Value));

    static readonly object instanceManagementLock = new();
    static readonly Dictionary<ExpressionInstancesKey, ActiveConstantExpression> expressionInstances = new(new ExpressionInstancesKeyComparer());
    static readonly Dictionary<InstancesKey, ActiveConstantExpression> instances = new();

    public static ActiveConstantExpression Create(ConstantExpression constantExpression, ActiveExpressionOptions? options)
    {
        var type = constantExpression.Type;
        var value = constantExpression.Value;
        if (typeof(Expression).IsAssignableFrom(type))
        {
            var key = new ExpressionInstancesKey((Expression)value, options);
            lock (instanceManagementLock)
            {
                if (!expressionInstances.TryGetValue(key, out var activeConstantExpression))
                {
                    activeConstantExpression = new ActiveConstantExpression(type, value, options, key, null);
                    expressionInstances.Add(key, activeConstantExpression);
                }
                ++activeConstantExpression.disposalCount;
                return activeConstantExpression;
            }
        }
        else
        {
            var key = new InstancesKey(type, value, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeConstantExpression))
                {
                    activeConstantExpression = new ActiveConstantExpression(type, value, options, null, key);
                    instances.Add(key, activeConstantExpression);
                }
                ++activeConstantExpression.disposalCount;
                return activeConstantExpression;
            }
        }
    }

    public static bool operator ==(ActiveConstantExpression a, ActiveConstantExpression b) =>
        a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveConstantExpression a, ActiveConstantExpression b) =>
        !(a == b);

    sealed record ExpressionInstancesKey(Expression? Expression, ActiveExpressionOptions? Options);

    sealed class ExpressionInstancesKeyComparer :
        IEqualityComparer<ExpressionInstancesKey>
    {
        public bool Equals(ExpressionInstancesKey x, ExpressionInstancesKey y) =>
            (x.Expression is null && y.Expression is null || x.Expression is not null && y.Expression is not null && ExpressionEqualityComparer.Default.Equals(x.Expression, y.Expression)) && (x.Options is null && y.Options is null || x.Options is not null && y.Options is not null && x.Options.Equals(y.Options));

        public int GetHashCode(ExpressionInstancesKey obj) =>
            HashCode.Combine(obj.Expression is null ? 0 : ExpressionEqualityComparer.Default.GetHashCode(obj.Expression), obj.Options);
    }

    sealed record InstancesKey(Type Type, object? Value, ActiveExpressionOptions? Options);
}
