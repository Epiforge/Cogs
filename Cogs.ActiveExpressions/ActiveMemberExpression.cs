namespace Cogs.ActiveExpressions;

class ActiveMemberExpression : ActiveExpression, IEquatable<ActiveMemberExpression>
{
    ActiveMemberExpression(CachedInstancesKey<MemberExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) : base(instancesKey.Expression, options, deferEvaluation) =>
        this.instancesKey = instancesKey;

    int disposalCount;
    bool doNotListenForPropertyChanges;
    ActiveExpression? expression;
    object? expressionValue;
    FastMethodInfo? fastGetter;
    FieldInfo? field;
    MethodInfo? getMethod;
    readonly CachedInstancesKey<MemberExpression> instancesKey;
    bool isFieldOfCompilerGeneratedType;
    MemberInfo? member;

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
            if (fastGetter is not null)
                UnsubscribeFromExpressionValueNotifications();
            else if (field is not null)
                UnsubscribeFromValueNotifications();
            if (expression is not null)
            {
                expression.PropertyChanged -= ExpressionPropertyChanged;
                expression.Dispose();
            }
        }
        return result;
    }

    public override bool Equals(object? obj) => obj is ActiveMemberExpression other && Equals(other);

    public bool Equals(ActiveMemberExpression other) => Equals(expression, other.expression) && member == other.member && Equals(options, other.options);

    protected override void Evaluate()
    {
        try
        {
            var expressionFault = expression?.Fault;
            if (expressionFault is not null)
                Fault = expressionFault;
            else
            {
                if (fastGetter is not null)
                {
                    var newExpressionValue = expression?.Value;
                    if (newExpressionValue != expressionValue)
                    {
                        UnsubscribeFromExpressionValueNotifications();
                        expressionValue = newExpressionValue;
                        SubscribeToExpressionValueNotifications();
                    }
                    Value = fastGetter.Invoke(expressionValue, emptyArray);
                }
                else if (field is not null)
                {
                    UnsubscribeFromValueNotifications();
                    Value = field.GetValue(expression?.Value);
                    SubscribeToValueNotifications();
                }
            }
        }
        catch (Exception ex)
        {
            Fault = ex;
        }
    }

    void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

    void ExpressionValuePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == member?.Name)
            Evaluate();
    }

    public override int GetHashCode() => HashCode.Combine(typeof(ActiveMemberExpression), expression, member, options);

    protected override bool GetShouldValueBeDisposed() => getMethod is not null && ApplicableOptions.IsMethodReturnValueDisposed(getMethod);

    protected override void Initialize()
    {
        try
        {
            if (instancesKey.Expression.Expression is not null)
            {
                expression = Create(instancesKey.Expression.Expression, options, IsDeferringEvaluation);
                expression.PropertyChanged += ExpressionPropertyChanged;
            }
            member = instancesKey.Expression.Member;
            switch (member)
            {
                case FieldInfo field:
                    this.field = field;
                    isFieldOfCompilerGeneratedType = expression?.Type.Name.StartsWith("<") ?? false;
                    break;
                case PropertyInfo property:
                    doNotListenForPropertyChanges = property.GetCustomAttribute<DoNotListenForPropertyChangesAttribute>() is not null;
                    getMethod = property.GetMethod;
                    fastGetter = FastMethodInfo.Get(getMethod);
                    isFieldOfCompilerGeneratedType = false;
                    break;
            }
            EvaluateIfNotDeferred();
        }
        catch (Exception ex)
        {
            DisposeValueIfNecessaryAndPossible();
            if (fastGetter is not null)
                UnsubscribeFromExpressionValueNotifications();
            else if (field is not null)
                UnsubscribeFromValueNotifications();
            if (expression is not null)
            {
                expression.PropertyChanged -= ExpressionPropertyChanged;
                expression.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public override string ToString() => $"{expression?.ToString() ?? member?.DeclaringType.FullName}.{member?.Name} {ToStringSuffix}";

    void SubscribeToExpressionValueNotifications()
    {
        if (doNotListenForPropertyChanges)
            return;
        if (expressionValue is INotifyPropertyChanged propertyChangedNotifier)
            propertyChangedNotifier.PropertyChanged += ExpressionValuePropertyChanged;
    }

    void SubscribeToValueNotifications()
    {
        if (isFieldOfCompilerGeneratedType)
        {
            if (ApplicableOptions.MemberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged && Value is INotifyDictionaryChanged dictionaryChangedNotifier)
                dictionaryChangedNotifier.DictionaryChanged += ValueChanged;
            else if (ApplicableOptions.MemberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged && Value is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged += ValueChanged;
        }
    }

    void UnsubscribeFromExpressionValueNotifications()
    {
        if (doNotListenForPropertyChanges)
            return;
        if (expressionValue is INotifyPropertyChanged propertyChangedNotifier)
            propertyChangedNotifier.PropertyChanged -= ExpressionValuePropertyChanged;
    }

    void UnsubscribeFromValueNotifications()
    {
        if (isFieldOfCompilerGeneratedType && TryGetUndeferredValue(out var value))
        {
            if (ApplicableOptions.MemberExpressionsListenToGeneratedTypesFieldValuesForDictionaryChanged && value is INotifyDictionaryChanged dictionaryChangedNotifier)
                dictionaryChangedNotifier.DictionaryChanged -= ValueChanged;
            else if (ApplicableOptions.MemberExpressionsListenToGeneratedTypesFieldValuesForCollectionChanged && value is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged -= ValueChanged;
        }
    }

    void ValueChanged(object sender, EventArgs e) => OnPropertyChanged(nameof(Value));

    static readonly object[] emptyArray = Array.Empty<object>();
    static readonly object instanceManagementLock = new();
    static readonly Dictionary<CachedInstancesKey<MemberExpression>, ActiveMemberExpression> instances = new(new CachedInstancesKeyComparer<MemberExpression>());

    public static ActiveMemberExpression Create(MemberExpression memberExpression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        var key = new CachedInstancesKey<MemberExpression>(memberExpression, options);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var activeMemberExpression))
            {
                activeMemberExpression = new ActiveMemberExpression(key, options, deferEvaluation);
                instances.Add(key, activeMemberExpression);
            }
            ++activeMemberExpression.disposalCount;
            return activeMemberExpression;
        }
    }

    public static bool operator ==(ActiveMemberExpression a, ActiveMemberExpression b) => a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveMemberExpression a, ActiveMemberExpression b) => !(a == b);
}
