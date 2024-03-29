namespace Cogs.ActiveExpressions;

sealed class ActiveIndexExpression :
    ActiveExpression,
    IEquatable<ActiveIndexExpression>,
    IObserveActiveExpressions<object?>
{
    ActiveIndexExpression(CachedInstancesKey<IndexExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) :
        base(instancesKey.Expression, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    EquatableList<ActiveExpression>? arguments;
    int disposalCount;
    FastMethodInfo? fastGetter;
    MethodInfo? getMethod;
    int? hashCode;
    PropertyInfo? indexer;
    readonly CachedInstancesKey<IndexExpression> instancesKey;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    ActiveExpression? @object;
    object? objectValue;

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
            UnsubscribeFromObjectValueNotifications();
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
        obj is ActiveIndexExpression other && Equals(other);

    public bool Equals(ActiveIndexExpression other) =>
        arguments == other.arguments && indexer == other.indexer && @object == other.@object && Equals(options, other.options);

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
            {
                var newObjectValue = @object?.Value;
                if (newObjectValue != objectValue)
                {
                    UnsubscribeFromObjectValueNotifications();
                    objectValue = newObjectValue;
                    SubscribeToObjectValueNotifications();
                }
                Value = fastGetter?.Invoke(objectValue, arguments?.Select(argument => argument.Value).ToArray() ?? Array.Empty<object?>());
            }
        }
        catch (Exception ex)
        {
            Fault = ex;
        }
    }

    public override int GetHashCode() =>
        hashCode ??= HashCode.Combine(typeof(ActiveIndexExpression), arguments, indexer, @object, options);

    protected override bool GetShouldValueBeDisposed() =>
        getMethod is not null && ApplicableOptions.IsMethodReturnValueDisposed(getMethod);

    protected override void Initialize()
    {
        var argumentsList = new List<ActiveExpression>();
        try
        {
            indexer = instancesKey.Expression.Indexer;
            getMethod = indexer.GetMethod;
            fastGetter = FastMethodInfo.Get(getMethod);
            @object = Create(instancesKey.Expression.Object, options, IsDeferringEvaluation);
            @object.AddActiveExpressionOserver(this);
            var indexExpressionArguments = instancesKey.Expression.Arguments;
            for (int i = 0, ii = indexExpressionArguments.Count; i < ii; ++i)
            {
                var indexExpressionArgument = indexExpressionArguments[i];
                var argument = Create(indexExpressionArgument, options, IsDeferringEvaluation);
                argument.AddActiveExpressionOserver(this);
                argumentsList.Add(argument);
            }
            arguments = new EquatableList<ActiveExpression>(argumentsList);
            EvaluateIfNotDeferred();
        }
        catch (Exception ex)
        {
            DisposeValueIfNecessaryAndPossible();
            UnsubscribeFromObjectValueNotifications();
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

    [SuppressMessage("Code Analysis", "CA1502: Avoid excessive complexity")]
    void ObjectValueCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    if (e.NewStartingIndex >= 0 && (e.NewItems?.Count ?? 0) > 0 && arguments?.Count == 1 && arguments?[0].Value is int index && e.NewStartingIndex <= index)
                        Evaluate();
                }
                break;
            case NotifyCollectionChangedAction.Move:
                {
                    var movingCount = Math.Max(e.OldItems?.Count ?? 0, e.NewItems?.Count ?? 0);
                    if (e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0 && movingCount > 0 && arguments?.Count == 1 && arguments?[0].Value is int index && (index >= e.OldStartingIndex && index < e.OldStartingIndex + movingCount || index >= e.NewStartingIndex && index < e.NewStartingIndex + movingCount))
                        Evaluate();
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    if (e.OldStartingIndex >= 0 && (e.OldItems?.Count ?? 0) > 0 && arguments?.Count == 1 && arguments?[0].Value is int index && e.OldStartingIndex <= index)
                        Evaluate();
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                {
                    if (arguments?.Count == 1 && arguments?[0].Value is int index)
                    {
                        var oldCount = e.OldItems?.Count ?? 0;
                        var newCount = e.NewItems?.Count ?? 0;
                        if (oldCount != newCount && (e.OldStartingIndex >= 0 || e.NewStartingIndex >= 0) && index >= Math.Min(Math.Max(e.OldStartingIndex, 0), Math.Max(e.NewStartingIndex, 0)) || e.OldStartingIndex >= 0 && index >= e.OldStartingIndex && index < e.OldStartingIndex + oldCount || e.NewStartingIndex >= 0 && index >= e.NewStartingIndex && index < e.NewStartingIndex + newCount)
                            Evaluate();
                    }
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                Evaluate();
                break;
        }
    }

    void ObjectValueDictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<object?, object?> e)
    {
        if (e.Action == NotifyDictionaryChangedAction.Reset)
            Evaluate();
        else if (arguments?.Count == 1)
        {
            var removed = false;
            var key = arguments?[0].Value;
            if (key is not null)
            {
                removed = e.OldItems?.Any(kv => key.Equals(kv.Key)) ?? false;
                var keyValuePair = e.NewItems?.FirstOrDefault(kv => key.Equals(kv.Key)) ?? default;
                if (keyValuePair.Key is not null)
                {
                    removed = false;
                    Value = keyValuePair.Value;
                }
            }
            if (removed)
                Fault = new KeyNotFoundException($"Key '{key}' was removed");
        }
    }

    void ObjectValuePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == indexer?.Name)
            Evaluate();
    }

    void SubscribeToObjectValueNotifications()
    {
        if (objectValue is INotifyDictionaryChanged dictionaryChangedNotifier)
            dictionaryChangedNotifier.DictionaryChanged += ObjectValueDictionaryChanged;
        else if (objectValue is INotifyCollectionChanged collectionChangedNotifier)
            collectionChangedNotifier.CollectionChanged += ObjectValueCollectionChanged;
        if (objectValue is INotifyPropertyChanged propertyChangedNotifier)
            propertyChangedNotifier.PropertyChanged += ObjectValuePropertyChanged;
    }

    public override string ToString() =>
        $"{@object}{string.Join(string.Empty, arguments?.Select(argument => $"[{argument}]"))} {ToStringSuffix}";

    void UnsubscribeFromObjectValueNotifications()
    {
        if (objectValue is INotifyDictionaryChanged dictionaryChangedNotifier)
            dictionaryChangedNotifier.DictionaryChanged -= ObjectValueDictionaryChanged;
        else if (objectValue is INotifyCollectionChanged collectionChangedNotifier)
            collectionChangedNotifier.CollectionChanged -= ObjectValueCollectionChanged;
        if (objectValue is INotifyPropertyChanged propertyChangedNotifier)
            propertyChangedNotifier.PropertyChanged -= ObjectValuePropertyChanged;
    }

    static readonly object instanceManagementLock = new();
    static readonly Dictionary<CachedInstancesKey<IndexExpression>, ActiveIndexExpression> instances = new(new CachedInstancesKeyComparer<IndexExpression>());

    public static ActiveIndexExpression Create(IndexExpression indexExpression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        var key = new CachedInstancesKey<IndexExpression>(indexExpression, options);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var activeIndexExpression))
            {
                activeIndexExpression = new ActiveIndexExpression(key, options, deferEvaluation);
                instances.Add(key, activeIndexExpression);
            }
            ++activeIndexExpression.disposalCount;
            return activeIndexExpression;
        }
    }

    public static bool operator ==(ActiveIndexExpression a, ActiveIndexExpression b) =>
        a.Equals(b);

    public static bool operator !=(ActiveIndexExpression a, ActiveIndexExpression b) =>
        !(a == b);
}
