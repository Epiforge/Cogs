namespace Cogs.ActiveExpressions;

sealed class ActiveMemberInitExpression :
    ActiveExpression,
    IEquatable<ActiveMemberInitExpression>,
    IObserveActiveExpressions<object?>
{
    public ActiveMemberInitExpression(CachedInstancesKey<MemberInitExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) :
        base(instancesKey.Expression, options, deferEvaluation)
    {
        this.instancesKey = instancesKey;
        memberInitExpression = instancesKey.Expression;
    }

    int disposalCount;
    int? hashCode;
    readonly CachedInstancesKey<MemberInitExpression> instancesKey;
    IReadOnlyDictionary<ActiveExpression, MemberInfo>? memberAssignmentExpressions;
    readonly MemberInitExpression memberInitExpression;
    [SuppressMessage("Usage", "CA2213: Disposable fields should be disposed", Justification = "This field will be disposed by the base class, the analyzer just doesn't see that.")]
    ActiveExpression? newExpression;

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
            if (newExpression is not null)
            {
                newExpression.RemoveActiveExpressionObserver(this);
                newExpression.Dispose();
            }
            if (memberAssignmentExpressions is not null)
                foreach (var memberAssignmentExpression in memberAssignmentExpressions.Keys)
                {
                    memberAssignmentExpression.RemoveActiveExpressionObserver(this);
                    memberAssignmentExpression.Dispose();
                }
        }
        return result;
    }

    public override bool Equals(object? obj) =>
        obj is ActiveMemberInitExpression other && Equals(other);

    public bool Equals(ActiveMemberInitExpression other) =>
        newExpression == other.newExpression && (memberAssignmentExpressions?.Select(kv => (memberName: kv.Value.Name, expression: kv.Key)) ?? Enumerable.Empty<(string memberName, ActiveExpression key)>()).OrderBy(t => t.memberName).SequenceEqual((other.memberAssignmentExpressions?.Select(kv => (memberName: kv.Value.Name, expression: kv.Key)) ?? Enumerable.Empty<(string memberName, ActiveExpression key)>()).OrderBy(t => t.memberName)) && Equals(options, other.options);

    protected override void Evaluate()
    {
        try
        {
            var newFault = newExpression?.Fault;
            var memberAssignmentFault = memberAssignmentExpressions?.Keys.Select(memberAssignmentExpression => memberAssignmentExpression.Fault).Where(fault => fault is not null).FirstOrDefault();
            if (newFault is not null)
                Fault = newFault;
            else if (memberAssignmentFault is not null)
                Fault = memberAssignmentFault;
            else
            {
                var val = newExpression?.Value;
                if (memberAssignmentExpressions is not null)
                    foreach (var kv in memberAssignmentExpressions)
                    {
                        if (kv.Value is FieldInfo field)
                            field.SetValue(val, kv.Key.Value);
                        else if (kv.Value is PropertyInfo property)
                            FastMethodInfo.Get(property.SetMethod).Invoke(val, new object?[] { kv.Key.Value });
                        else
                            throw new NotSupportedException("Cannot handle member that is not a field or property");
                    }
                Value = val;
            }
        }
        catch (Exception ex)
        {
            Fault = ex;
        }
    }

    int GenerateHashCode()
    {
        var hashCode = HashCode.Combine(typeof(ActiveMemberInitExpression), newExpression, options);
        if (memberAssignmentExpressions is not null)
            foreach (var memberAssignmentExpression in memberAssignmentExpressions)
                hashCode = HashCode.Combine(hashCode, memberAssignmentExpression.Key, memberAssignmentExpression.Value);
        return hashCode;
    }

    public override int GetHashCode() =>
        hashCode ??= GenerateHashCode();

    protected override bool GetShouldValueBeDisposed() =>
        ApplicableOptions.IsConstructedTypeDisposed(memberInitExpression.NewExpression.Constructor);

    protected override void Initialize()
    {
        if (memberInitExpression.NewExpression.Type.IsValueType)
            throw new NotSupportedException("Member initialization expressions of value types are not supported");
        var memberAssignmentExpressions = new Dictionary<ActiveExpression, MemberInfo>();
        try
        {
            newExpression = Create(memberInitExpression.NewExpression, options, IsDeferringEvaluation);
            newExpression.AddActiveExpressionOserver(this);
            var bindings = memberInitExpression.Bindings;
            for (int i = 0, ii = bindings.Count; i < ii; ++i)
            {
                var binding = bindings[i];
                if (binding is MemberAssignment memberAssignmentBinding)
                {
                    var memberAssignmentExpression = Create(memberAssignmentBinding.Expression, options, IsDeferringEvaluation);
                    memberAssignmentExpressions.Add(memberAssignmentExpression, memberAssignmentBinding.Member);
                    memberAssignmentExpression.AddActiveExpressionOserver(this);
                }
                else
                    throw new NotSupportedException("Only member assignment bindings are supported in member init expressions");
            }
            this.memberAssignmentExpressions = memberAssignmentExpressions;
            EvaluateIfNotDeferred();
        }
        catch (Exception ex)
        {
            DisposeValueIfNecessaryAndPossible();
            if (newExpression is not null)
            {
                newExpression.RemoveActiveExpressionObserver(this);
                newExpression.Dispose();
            }
            foreach (var memberAssignmentExpression in memberAssignmentExpressions.Keys)
            {
                memberAssignmentExpression.RemoveActiveExpressionObserver(this);
                memberAssignmentExpression.Dispose();
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    void IObserveActiveExpressions<object?>.ActiveExpressionChanged(IObservableActiveExpression<object?> activeExpression, object? oldValue, object? newValue, Exception? oldFault, Exception? newFault)
    {
        if (ReferenceEquals(activeExpression, newExpression))
            Evaluate();
        else if (activeExpression is ActiveExpression memberAssignmentExpression && (memberAssignmentExpressions?.TryGetValue(memberAssignmentExpression, out var member) ?? false))
        {
            if (newFault is not null)
                Fault = newFault;
            else
            {
                var intactValue = TryGetUndeferredValue(out var val) && val is not null;
                if (!intactValue)
                    val = newExpression?.Value;
                if (val is not null)
                {
                    if (member is FieldInfo field)
                        field.SetValue(val, memberAssignmentExpression.Value);
                    else if (member is PropertyInfo property)
                        FastMethodInfo.Get(property.SetMethod).Invoke(val, new object?[] { memberAssignmentExpression.Value });
                    else
                        throw new NotSupportedException("Cannot handle member that is not a field or property");
                }
                if (!intactValue)
                    Value = val;
            }
        }
    }

    public override string ToString() =>
        $"{newExpression} {{ {string.Join(", ", memberAssignmentExpressions.Select(kv => $"{kv.Value.Name} = {kv.Key}"))} }} {ToStringSuffix}";

    static readonly object instanceManagementLock = new();
    static readonly Dictionary<CachedInstancesKey<MemberInitExpression>, ActiveMemberInitExpression> instances = new(new CachedInstancesKeyComparer<MemberInitExpression>());

    public static ActiveMemberInitExpression Create(MemberInitExpression memberInitExpression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        var key = new CachedInstancesKey<MemberInitExpression>(memberInitExpression, options);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var activeMemberInitExpression))
            {
                activeMemberInitExpression = new ActiveMemberInitExpression(key, options, deferEvaluation);
                instances.Add(key, activeMemberInitExpression);
            }
            ++activeMemberInitExpression.disposalCount;
            return activeMemberInitExpression;
        }
    }

    public static bool operator ==(ActiveMemberInitExpression a, ActiveMemberInitExpression b) =>
        a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveMemberInitExpression a, ActiveMemberInitExpression b) =>
        !(a == b);
}
