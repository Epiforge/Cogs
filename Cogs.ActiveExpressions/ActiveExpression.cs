namespace Cogs.ActiveExpressions;

/// <summary>
/// Provides the base class from which the classes that represent active expression tree nodes are derived; use <see cref="Create{TResult}(LambdaExpression, object[])"/> or one of its overloads to create an active expression
/// </summary>
public abstract class ActiveExpression :
    SyncDisposable,
    IObservableActiveExpression<object?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveExpression"/> class
    /// </summary>
    /// <param name="expression">The expression upon which the active expression is based</param>
    /// <param name="options">The <see cref="ActiveExpressionOptions"/> instance of this node</param>
    /// <param name="deferEvaluation"><c>true</c> if evaluation should be deferred until the <see cref="Value"/> property is accessed; otherwise, <c>false</c></param>
    protected ActiveExpression(Expression expression, ActiveExpressionOptions? options, bool deferEvaluation) :
        this(expression is null ? throw new ArgumentNullException(nameof(expression)) : expression.Type, expression.NodeType, options, deferEvaluation)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveExpression"/> class
    /// </summary>
    /// <param name="type">The <see cref="System.Type"/> for all possible values of this node</param>
    /// <param name="nodeType">The <see cref="ExpressionType"/> for this node</param>
    /// <param name="options">The <see cref="ActiveExpressionOptions"/> instance of this node</param>
    /// <param name="deferEvaluation"><c>true</c> if evaluation should be deferred until the <see cref="Value"/> property is accessed; otherwise, <c>false</c></param>
    protected ActiveExpression(Type type, ExpressionType nodeType, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        Type = type;
        defaultValue = FastDefault.Get(type);
        val = defaultValue;
        valueEqualityComparer = FastEqualityComparer.Get(type);
        NodeType = nodeType;
        this.options = options;
        deferringEvaluation = deferEvaluation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveExpression"/> class
    /// </summary>
    /// <param name="type">The <see cref="System.Type"/> for all possible values of this node</param>
    /// <param name="nodeType">The <see cref="ExpressionType"/> for this node</param>
    /// <param name="options">The <see cref="ActiveExpressionOptions"/> instance of this node</param>
    /// <param name="value">The value of this node</param>
    protected ActiveExpression(Type type, ExpressionType nodeType, ActiveExpressionOptions? options, object value) :
        this(type, nodeType, options, false) => val = value;

    readonly object? defaultValue;
    bool deferringEvaluation;
    readonly object deferringEvaluationAccess = new();
    Exception? fault;
    readonly object initializationAccess = new();
    Exception? initializationException;
    bool isInitialized = false;
    readonly List<IObserveActiveExpressions<object?>> observers = new();
    readonly object observersAccess = new();
    IReadOnlyList<IObserveActiveExpressions<object?>> observersCopy = Array.Empty<IObserveActiveExpressions<object?>>();
    bool observersCopyIsValid = true;
    object? val;
    readonly FastEqualityComparer valueEqualityComparer;

    /// <summary>
    /// The <see cref="ActiveExpressionOptions"/> instance for this node
    /// </summary>
    protected readonly ActiveExpressionOptions? options;

    /// <summary>
    /// Gets the currently applicable instance of <see cref="ActiveExpressionOptions"/> for this node
    /// </summary>
    protected ActiveExpressionOptions ApplicableOptions =>
        options ?? ActiveExpressionOptions.Default;

    /// <summary>
    /// Gets/sets the current fault for this node
    /// </summary>
    public Exception? Fault
    {
        get
        {
            EvaluateIfDeferred();
            return fault;
        }
        set
        {
            var oldValue = val;
            var oldFault = fault;
            var valueChanged = SetBackedProperty(ref val, in defaultValue, valueChangingEventArgs, valueChangedEventArgs);
            var faultChanged = SetBackedProperty(ref fault, in value, faultChangingEventArgs, faultChangedEventArgs);
            if (valueChanged || faultChanged)
                NotifyObservers(oldValue, val, oldFault, fault);
        }
    }

    /// <summary>
    /// Gets whether evaluation is being deferred
    /// </summary>
    protected bool IsDeferringEvaluation
    {
        get
        {
            lock (deferringEvaluationAccess)
                return deferringEvaluation;
        }
    }

    /// <summary>
    /// Gets the <see cref="ExpressionType"/> for this node
    /// </summary>
    public ExpressionType NodeType { get; }

    /// <summary>
    /// Gets the suffix of the string representation of this node
    /// </summary>
    protected string ToStringSuffix =>
        $"/* {GetValueString(fault, !TryGetUndeferredValue(out var value), value)} */";

    /// <summary>
    /// Gets the <see cref="System.Type"/> for possible values of this node
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets/sets the current value for this node
    /// </summary>
    public object? Value
    {
        get
        {
            EvaluateIfDeferred();
            return val;
        }
        set
        {
            var oldValue = val;
            var oldFault = fault;
            var faultChanged = SetBackedProperty(ref fault, null, faultChangingEventArgs, faultChangedEventArgs);
            var valueChanged = !valueEqualityComparer.Equals(value, val);
            if (valueChanged)
            {
                var previousValue = val;
                OnPropertyChanging(valueChangingEventArgs);
                val = value;
                OnPropertyChanged(valueChangedEventArgs);
                DisposeIfNecessaryAndPossible(previousValue);
            }
            if (valueChanged || faultChanged)
                NotifyObservers(oldValue, val, oldFault, fault);
        }
    }

    /// <inheritdoc/>
    public void AddActiveExpressionOserver(IObserveActiveExpressions<object?> observer)
    {
        lock (observersAccess)
        {
            observers.Add(observer);
            observersCopyIsValid = false;
        }
    }

    void NotifyObservers(object? oldValue, object? newValue, Exception? oldFault, Exception? newFault)
    {
        lock (observersAccess)
        {
            if (!observersCopyIsValid)
            {
                observersCopy = observers.ToImmutableArray();
                observersCopyIsValid = true;
            }
        }
        for (int i = 0, ii = observersCopy.Count; i < ii; ++i)
            observersCopy[i].ActiveExpressionChanged(this, oldValue, newValue, oldFault, newFault);
    }

    void DisposeIfNecessaryAndPossible(object? value)
    {
        if ((value is IDisposable || value is IAsyncDisposable) && GetShouldValueBeDisposed())
        {
            if (!ApplicableOptions.PreferAsyncDisposal && value is IDisposable preferredDisposable)
                preferredDisposable.Dispose();
            else if (value is IAsyncDisposable asyncDisposable)
            {
                if (ApplicableOptions.BlockOnAsyncDisposal)
                    asyncDisposable.DisposeAsync().AsTask().Wait();
                else
                    Task.Run(async () => await asyncDisposable.DisposeAsync().ConfigureAwait(false));
            }
            else if (value is IDisposable disposable)
                disposable.Dispose();
        }
    }

    /// <summary>
    /// Disposes of the expression's value if necessary and possible (intended to be called within <see cref="SyncDisposable.Dispose(bool)"/>)
    /// </summary>
    protected void DisposeValueIfNecessaryAndPossible() =>
        DisposeIfNecessaryAndPossible(val);

    /// <summary>
    /// Throws a <see cref="NotImplementedException"/> because deriving classes should be overriding this method
    /// </summary>
    /// <param name="obj">The object to compare with the current object</param>
    public override bool Equals(object? obj) =>
        throw new NotImplementedException();

    /// <summary>
    /// Evaluates the current node
    /// </summary>
    protected virtual void Evaluate()
    {
    }

    void EvaluateIfDeferred()
    {
        lock (deferringEvaluationAccess)
        {
            if (deferringEvaluation)
            {
                deferringEvaluation = false;
                Evaluate();
            }
        }
    }

    /// <summary>
    /// Evaluates this node if its evaluation is not deferred
    /// </summary>
    protected void EvaluateIfNotDeferred()
    {
        lock (deferringEvaluationAccess)
        {
            if (!deferringEvaluation)
                Evaluate();
        }
    }

    /// <summary>
    /// Throws a <see cref="NotImplementedException"/> because deriving classes should be overriding this method
    /// </summary>
    public override int GetHashCode() =>
        throw new NotImplementedException();

    /// <summary>
    /// Gets whether a value produced by this expression should be disposed
    /// </summary>
    /// <returns><c>true</c> if values from this expression should be disposed; otherwise, <c>false</c></returns>
    protected virtual bool GetShouldValueBeDisposed() =>
        false;

    /// <summary>
    /// Called after an active expression is created in order to initialize it
    /// </summary>
    protected abstract void Initialize();

    /// <inheritdoc/>
    public void RemoveActiveExpressionObserver(IObserveActiveExpressions<object?> observer)
    {
        lock (observersAccess)
        {
            observers.Remove(observer);
            observersCopyIsValid = false;
        }
    }

    /// <summary>
    /// Attempts to get this node's value if its evaluation is not deferred
    /// </summary>
    /// <param name="value">The value if evaluation is not deferred</param>
    /// <returns><c>true</c> if evaluation is not deferred and <paramref name="value"/> has been set to the value; otherwise, <c>false</c></returns>
    protected bool TryGetUndeferredValue(out object? value)
    {
        lock (deferringEvaluationAccess)
        {
            if (deferringEvaluation)
            {
                value = null;
                return false;
            }
        }
        value = val;
        return true;
    }

    static readonly PropertyChangedEventArgs faultChangedEventArgs = new(nameof(Fault));
    static readonly PropertyChangingEventArgs faultChangingEventArgs = new(nameof(Fault));
    static readonly ConcurrentDictionary<MethodInfo, PropertyInfo> propertyGetMethodToProperty = new(); // NCrunch: no coverage
    static readonly PropertyChangedEventArgs valueChangedEventArgs = new(nameof(Value));
    static readonly PropertyChangingEventArgs valueChangingEventArgs = new(nameof(Value));

    /// <summary>
    /// Gets/sets the method that will be invoked during the active expression creation process to optimize expressions (default is null)
    /// </summary>
    public static Func<Expression, Expression>? Optimizer { get; set; }

    /// <summary>
    /// Returns a task which is only completed when the specified condition evaluates to <c>true</c>
    /// </summary>
    /// <param name="condition">The condition</param>
    public static Task ConditionAsync(Expression<Func<bool>> condition) =>
        ConditionAsync(condition, null, CancellationToken.None);

    /// <summary>
    /// Returns a task which is only completed when the specified condition evaluates to <c>true</c>
    /// </summary>
    /// <param name="condition">The condition</param>
    /// <param name="cancellationToken">A token which may cancel awaiting the condition</param>
    public static Task ConditionAsync(Expression<Func<bool>> condition, CancellationToken cancellationToken) =>
        ConditionAsync(condition, null, cancellationToken);

    /// <summary>
    /// Returns a task which is only completed when the specified condition evaluates to <c>true</c>
    /// </summary>
    /// <param name="condition">The condition</param>
    /// <param name="options">Options to use when creating the active expression of the condition</param>
    public static Task ConditionAsync(Expression<Func<bool>> condition, ActiveExpressionOptions? options) =>
        ConditionAsync(condition, options, CancellationToken.None);

    /// <summary>
    /// Returns a task which is only completed when the specified condition evaluates to <c>true</c>
    /// </summary>
    /// <param name="condition">The condition</param>
    /// <param name="options">Options to use when creating the active expression of the condition</param>
    /// <param name="cancellationToken">A token which may cancel awaiting the condition</param>
    [SuppressMessage("Reliability", "CA2000: Dispose objects before losing scope")]
    public static Task ConditionAsync(Expression<Func<bool>> condition, ActiveExpressionOptions? options, CancellationToken cancellationToken)
    {
        var taskCompletionSource = new TaskCompletionSource<object?>();
        IActiveExpression<bool>? activeExpression = null;
        void cancellationTokenCancelled()
        {
            activeExpression.PropertyChanged -= propertyChangedHandler;
            activeExpression.Dispose();
            taskCompletionSource.SetCanceled();
        }
        void propertyChangedHandler(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IActiveExpression<bool>.Value) && activeExpression!.Value)
            {
                activeExpression.PropertyChanged -= propertyChangedHandler;
                activeExpression.Dispose();
                taskCompletionSource!.SetResult(null);
            }
            else if (e.PropertyName == nameof(IActiveExpression<bool>.Fault) && activeExpression!.Fault is { } fault)
            {
                activeExpression.PropertyChanged -= propertyChangedHandler;
                activeExpression.Dispose();
                taskCompletionSource!.SetException(fault);
            }
        }
        if (cancellationToken.CanBeCanceled && cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        activeExpression = Create(condition, options);
        if (activeExpression.Value)
        {
            activeExpression.Dispose();
            return Task.CompletedTask;
        }
        else if (activeExpression.Fault is { } fault)
        {
            activeExpression.Dispose();
            return Task.FromException(fault);
        }
        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(cancellationTokenCancelled);
        activeExpression.PropertyChanged += propertyChangedHandler;
        return taskCompletionSource.Task;
    }

    [SuppressMessage("Code Analysis", "CA2000: Dispose objects before losing scope", Justification = "True, but it will be disposed elsewhere")]
    internal static ActiveExpression Create(Expression? expression, ActiveExpressionOptions? options, bool deferEvaluation)
    {
        ActiveExpression activeExpression = expression switch
        {
            BinaryExpression binaryExpression => ActiveBinaryExpression.Create(binaryExpression, options, deferEvaluation),
            ConditionalExpression conditionalExpression => ActiveConditionalExpression.Create(conditionalExpression, options, deferEvaluation),
            ConstantExpression constantExpression => ActiveConstantExpression.Create(constantExpression, options),
            InvocationExpression invocationExpression => ActiveInvocationExpression.Create(invocationExpression, options, deferEvaluation),
            IndexExpression indexExpression => ActiveIndexExpression.Create(indexExpression, options, deferEvaluation),
            MemberExpression memberExpression => ActiveMemberExpression.Create(memberExpression, options, deferEvaluation),
            MemberInitExpression memberInitExpression => ActiveMemberInitExpression.Create(memberInitExpression, options, deferEvaluation),
            MethodCallExpression methodCallExpressionForPropertyGet when propertyGetMethodToProperty.GetOrAdd(methodCallExpressionForPropertyGet.Method, GetPropertyFromGetMethod) is PropertyInfo property =>
                methodCallExpressionForPropertyGet.Arguments.Count > 0
                ?
                ActiveIndexExpression.Create(Expression.MakeIndex(methodCallExpressionForPropertyGet.Object, property, methodCallExpressionForPropertyGet.Arguments), options, deferEvaluation)
                :
                ActiveMemberExpression.Create(Expression.MakeMemberAccess(methodCallExpressionForPropertyGet.Object, property), options, deferEvaluation),
            MethodCallExpression methodCallExpression => ActiveMethodCallExpression.Create(methodCallExpression, options, deferEvaluation),
            NewArrayExpression newArrayInitExpression when newArrayInitExpression.NodeType == ExpressionType.NewArrayInit =>
                ActiveNewArrayInitExpression.Create(newArrayInitExpression, options, deferEvaluation),
            NewExpression newExpression => ActiveNewExpression.Create(newExpression, options, deferEvaluation),
            TypeBinaryExpression typeBinaryExpression => ActiveTypeBinaryExpression.Create(typeBinaryExpression, options, deferEvaluation),
            UnaryExpression unaryExpression when unaryExpression.NodeType == ExpressionType.Quote =>
                ActiveConstantExpression.Create(Expression.Constant(unaryExpression.Operand), options),
            UnaryExpression unaryExpression => ActiveUnaryExpression.Create(unaryExpression, options, deferEvaluation),
            _ => throw new NotSupportedException()
        };
        lock (activeExpression.initializationAccess)
        {
            if (activeExpression.initializationException is not null)
            {
                ExceptionDispatchInfo.Capture(activeExpression.initializationException).Throw();
                throw activeExpression.initializationException;
            }
            try
            {
                if (!activeExpression.isInitialized)
                {
                    activeExpression.Initialize();
                    activeExpression.isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                activeExpression.initializationException = ex;
                activeExpression.Dispose();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }
        if (!deferEvaluation)
            activeExpression.EvaluateIfDeferred();
        return activeExpression;
    }

    /// <summary>
    /// Creates an active expression using a specified lambda expression and arguments
    /// </summary>
    /// <typeparam name="TResult">The type that <paramref name="lambdaExpression"/> returns</typeparam>
    /// <param name="lambdaExpression">The lambda expression</param>
    /// <param name="arguments">The arguments</param>
    /// <returns>The active expression</returns>
    public static ActiveExpression<TResult> Create<TResult>(LambdaExpression lambdaExpression, params object?[] arguments) =>
        CreateWithOptions<TResult>(lambdaExpression, null, arguments);

    /// <summary>
    /// Creates an active expression using a specified lambda expression, options and arguments
    /// </summary>
    /// <typeparam name="TResult">The type that <paramref name="lambdaExpression"/> returns</typeparam>
    /// <param name="lambdaExpression">The lambda expression</param>
    /// <param name="options">Active expression options to use instead of <see cref="ActiveExpressionOptions.Default"/></param>
    /// <param name="arguments">The arguments</param>
    /// <returns>The active expression</returns>
    public static ActiveExpression<TResult> CreateWithOptions<TResult>(LambdaExpression lambdaExpression, ActiveExpressionOptions? options, params object?[] arguments)
    {
        options?.Freeze();
        return ActiveExpression<TResult>.Create(lambdaExpression, options, arguments);
    }

    /// <summary>
    /// Creates an active expression using a specified strongly-typed lambda expression with no arguments
    /// </summary>
    /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
    /// <param name="expression">The strongly-typed lambda expression</param>
    /// <param name="options">Active expression options to use instead of <see cref="ActiveExpressionOptions.Default"/></param>
    /// <returns>The active expression</returns>
    public static ActiveExpression<TResult> Create<TResult>(Expression<Func<TResult>> expression, ActiveExpressionOptions? options = null)
    {
        options?.Freeze();
        return ActiveExpression<TResult>.Create(expression, options);
    }

    /// <summary>
    /// Creates an active expression using a specified strongly-typed lambda expression and one argument
    /// </summary>
    /// <typeparam name="TArg">The type of the argument.</typeparam>
    /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
    /// <param name="expression">The strongly-typed lambda expression</param>
    /// <param name="arg">The argument</param>
    /// <param name="options">Active expression options to use instead of <see cref="ActiveExpressionOptions.Default"/></param>
    /// <returns>The active expression</returns>
    public static ActiveExpression<TArg, TResult> Create<TArg, TResult>(Expression<Func<TArg, TResult>> expression, TArg arg, ActiveExpressionOptions? options = null)
    {
        options?.Freeze();
        return ActiveExpression<TArg, TResult>.Create(expression, arg, options);
    }

    /// <summary>
    /// Creates an active expression using a specified strongly-typed lambda expression and two arguments
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument</typeparam>
    /// <typeparam name="TArg2">The type of the second argument</typeparam>
    /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
    /// <param name="expression">The strongly-typed lambda expression</param>
    /// <param name="arg1">The first argument</param>
    /// <param name="arg2">The second argument</param>
    /// <param name="options">Active expression options to use instead of <see cref="ActiveExpressionOptions.Default"/></param>
    /// <returns>The active expression</returns>
    public static ActiveExpression<TArg1, TArg2, TResult> Create<TArg1, TArg2, TResult>(Expression<Func<TArg1, TArg2, TResult>> expression, TArg1 arg1, TArg2 arg2, ActiveExpressionOptions? options = null)
    {
        options?.Freeze();
        return ActiveExpression<TArg1, TArg2, TResult>.Create(expression, arg1, arg2, options);
    }

    /// <summary>
    /// Creates an active expression using a specified strongly-typed lambda expression and three arguments
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument</typeparam>
    /// <typeparam name="TArg2">The type of the second argument</typeparam>
    /// <typeparam name="TArg3">The type of the third argument</typeparam>
    /// <typeparam name="TResult">The type that <paramref name="expression"/> returns</typeparam>
    /// <param name="expression">The strongly-typed lambda expression</param>
    /// <param name="arg1">The first argument</param>
    /// <param name="arg2">The second argument</param>
    /// <param name="arg3">The third argument</param>
    /// <param name="options">Active expression options to use instead of <see cref="ActiveExpressionOptions.Default"/></param>
    /// <returns>The active expression</returns>
    public static ActiveExpression<TArg1, TArg2, TArg3, TResult> Create<TArg1, TArg2, TArg3, TResult>(Expression<Func<TArg1, TArg2, TArg3, TResult>> expression, TArg1 arg1, TArg2 arg2, TArg3 arg3, ActiveExpressionOptions? options = null)
    {
        options?.Freeze();
        return ActiveExpression<TArg1, TArg2, TArg3, TResult>.Create(expression, arg1, arg2, arg3, options);
    }

    /// <summary>
    /// Produces a human-readable representation of an expression
    /// </summary>
    /// <param name="expressionType">The type of expression</param>
    /// <param name="resultType">The type of the result when the expression is evaluated</param>
    /// <param name="operands">The operands (or arguments) of the expression</param>
    /// <returns>A human-readable representation of the expression</returns>
    public static string GetOperatorExpressionSyntax(ExpressionType expressionType, Type resultType, params object?[] operands) =>
        resultType is null
        ?
        throw new ArgumentNullException(nameof(resultType))
        :
        operands is null
        ?
        throw new ArgumentNullException(nameof(operands))
        :
        expressionType switch
        {
            ExpressionType.Add => $"({operands[0]} + {operands[1]})",
            ExpressionType.AddChecked => $"checked({operands[0]} + {operands[1]})",
            ExpressionType.And => $"({operands[0]} & {operands[1]})",
            ExpressionType.Convert => $"(({resultType.FullName}){operands[0]})",
            ExpressionType.ConvertChecked => $"checked(({resultType.FullName}){operands[0]})",
            ExpressionType.Decrement => $"({operands[0]} - 1)",
            ExpressionType.Divide => $"({operands[0]} / {operands[1]})",
            ExpressionType.Equal => $"({operands[0]} == {operands[1]})",
            ExpressionType.ExclusiveOr => $"({operands[0]} ^ {operands[1]})",
            ExpressionType.GreaterThan => $"({operands[0]} > {operands[1]})",
            ExpressionType.GreaterThanOrEqual => $"({operands[0]} >= {operands[1]})",
            ExpressionType.Increment => $"({operands[0]} + 1)",
            ExpressionType.LeftShift => $"({operands[0]} << {operands[1]})",
            ExpressionType.LessThan => $"({operands[0]} < {operands[1]})",
            ExpressionType.LessThanOrEqual => $"({operands[0]} <= {operands[1]})",
            ExpressionType.Modulo => $"({operands[0]} % {operands[1]})",
            ExpressionType.Multiply => $"({operands[0]} * {operands[1]})",
            ExpressionType.MultiplyChecked => $"checked({operands[0]} * {operands[1]})",
            ExpressionType.Negate => $"(-{operands[0]})",
            ExpressionType.NegateChecked => $"checked(-{operands[0]})",
            ExpressionType.Not when operands[0] is bool || operands[0] is ActiveExpression notOperand && (notOperand.Type == typeof(bool) || notOperand.Type == typeof(bool?)) => $"(!{operands[0]})",
            ExpressionType.Not or ExpressionType.OnesComplement => $"(~{operands[0]})",
            ExpressionType.NotEqual => $"({operands[0]} != {operands[1]})",
            ExpressionType.Or => $"({operands[0]} | {operands[1]})",
            ExpressionType.Power => $"{nameof(Math)}.{nameof(Math.Pow)}({operands[0]}, {operands[1]})",
            ExpressionType.RightShift => $"({operands[0]} >> {operands[1]})",
            ExpressionType.Subtract => $"({operands[0]} - {operands[1]})",
            ExpressionType.SubtractChecked => $"checked({operands[0]} - {operands[1]})",
            ExpressionType.TypeIs => $"({operands[0]} is {operands[1]})",
            ExpressionType.UnaryPlus => $"(+{operands[0]})",
            _ => throw new ArgumentOutOfRangeException(nameof(expressionType)),
        };

    static PropertyInfo GetPropertyFromGetMethod(MethodInfo getMethod) =>
        getMethod.DeclaringType.GetRuntimeProperties().FirstOrDefault(property => property.GetMethod == getMethod);

    /// <summary>
    /// Gets the string representation for the value of a node
    /// </summary>
    /// <param name="fault">The fault for the node</param>
    /// <param name="deferred"><c>true</c> if evaluation of the node has been deferred; otherwise, <c>false</c></param>
    /// <param name="value">The value for the node</param>
    /// <returns>The string representation of the node's value</returns>
    protected static string GetValueString(Exception? fault, bool deferred, object? value)
    {
        if (fault is not null)
            return $"[{fault.GetType().Name}: {fault.Message}]";
        if (deferred)
            return "?";
        if (value is string str)
        {
            var sb = new StringBuilder(str);
            sb.Replace("\\", "\\\\");
            sb.Replace("\0", "\\0");
            sb.Replace("\a", "\\a");
            sb.Replace("\b", "\\b");
            sb.Replace("\f", "\\f");
            sb.Replace("\n", "\\n");
            sb.Replace("\r", "\\r");
            sb.Replace("\t", "\\t");
            sb.Replace("\v", "\\v");
            return $"\"{sb}\"";
        }
        return value switch
        {
            char ch => ch switch
            {
                '\\' => "'\\\\'",
                '\0' => "'\\0'",
                '\a' => "'\\a'",
                '\b' => "'\\b'",
                '\f' => "'\\f'",
                '\n' => "'\\n'",
                '\r' => "'\\r'",
                '\t' => "'\\t'",
                '\v' => "'\\v'",
                _ => $"'{ch}'",
            },
            DateTime dt => $"new System.DateTime({dt.Ticks}, System.DateTimeKind.{dt.Kind})",
            Guid guid => $"new System.Guid(\"{guid}\")",
            TimeSpan ts => $"new System.TimeSpan({ts.Ticks})",
            null => "null",
            _ => $"{value}"
        };
    }

    internal static Expression? ReplaceParameters(LambdaExpression lambdaExpression, params object?[] arguments)
    {
        var parameterTranslation = new Dictionary<ParameterExpression, ConstantExpression>();
        lambdaExpression = (LambdaExpression)(Optimizer?.Invoke(lambdaExpression) ?? lambdaExpression);
        for (var i = 0; i < lambdaExpression.Parameters.Count; ++i)
        {
            var parameter = lambdaExpression.Parameters[i];
            var constant = Expression.Constant(arguments[i], parameter.Type);
            parameterTranslation.Add(parameter, constant);
        }
        return ReplaceParameters(parameterTranslation, lambdaExpression.Body);
    }

    static Expression? ReplaceParameters(Dictionary<ParameterExpression, ConstantExpression> parameterTranslation, Expression expression)
    {
        switch (expression)
        {
            case BinaryExpression binaryExpression:
                return Expression.MakeBinary(binaryExpression.NodeType, ReplaceParameters(parameterTranslation, binaryExpression.Left), ReplaceParameters(parameterTranslation, binaryExpression.Right), binaryExpression.IsLiftedToNull, binaryExpression.Method, binaryExpression.Conversion);
            case ConditionalExpression conditionalExpression:
                return Expression.Condition(ReplaceParameters(parameterTranslation, conditionalExpression.Test), ReplaceParameters(parameterTranslation, conditionalExpression.IfTrue), ReplaceParameters(parameterTranslation, conditionalExpression.IfFalse), conditionalExpression.Type);
            case ConstantExpression constantExpression:
                return constantExpression;
            case InvocationExpression invocationExpression:
                return Expression.Invoke(ReplaceParameters(parameterTranslation, invocationExpression.Expression), invocationExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument)).ToArray());
            case IndexExpression indexExpression:
                return Expression.MakeIndex(ReplaceParameters(parameterTranslation, indexExpression.Object), indexExpression.Indexer, indexExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument)));
            case LambdaExpression lambdaExpression:
                return lambdaExpression;
            case MemberExpression memberExpression:
                return Expression.MakeMemberAccess(ReplaceParameters(parameterTranslation, memberExpression.Expression), memberExpression.Member);
            case MemberInitExpression memberInitExpression:
                return Expression.MemberInit((NewExpression)ReplaceParameters(parameterTranslation, memberInitExpression.NewExpression)!, memberInitExpression.Bindings.Cast<MemberAssignment>().Select(memberAssignment => memberAssignment.Update(ReplaceParameters(parameterTranslation, memberAssignment.Expression))).ToArray());
            case MethodCallExpression methodCallExpression:
                return methodCallExpression.Object is null ? Expression.Call(methodCallExpression.Method, methodCallExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument))) : Expression.Call(ReplaceParameters(parameterTranslation, methodCallExpression.Object), methodCallExpression.Method, methodCallExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument)));
            case NewArrayExpression newArrayInitExpression when newArrayInitExpression.NodeType == ExpressionType.NewArrayInit:
                return Expression.NewArrayInit(newArrayInitExpression.Type.GetElementType(), newArrayInitExpression.Expressions.Select(expression => ReplaceParameters(parameterTranslation, expression)));
            case NewExpression newExpression:
                var newArguments = newExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument));
                return newExpression.Constructor is null ? newExpression : newExpression.Members is null ? Expression.New(newExpression.Constructor, newArguments) : Expression.New(newExpression.Constructor, newArguments, newExpression.Members);
            case ParameterExpression parameterExpression:
                return parameterTranslation[parameterExpression];
            case TypeBinaryExpression typeBinaryExpression:
                return Expression.TypeIs(ReplaceParameters(parameterTranslation, typeBinaryExpression.Expression), typeBinaryExpression.TypeOperand);
            case UnaryExpression unaryExpression:
                return Expression.MakeUnary(unaryExpression.NodeType, ReplaceParameters(parameterTranslation, unaryExpression.Operand), unaryExpression.Type, unaryExpression.Method);
            case null:
                return null;
            default:
                throw new NotSupportedException($"Cannot replace parameters in {expression.GetType().Name}");
        }
    }

    /// <summary>
    /// Determines whether two active expression tree nodes are the same
    /// </summary>
    /// <param name="a">The first node to compare, or null</param>
    /// <param name="b">The second node to compare, or null</param>
    /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
    [SuppressMessage("Code Analysis", "CA1502: Avoid excessive complexity")]
    public static bool operator ==(ActiveExpression? a, ActiveExpression? b) =>
        a is ActiveAndAlsoExpression andAlsoA && b is ActiveAndAlsoExpression andAlsoB
        ?
        andAlsoA == andAlsoB
        :
        a is ActiveCoalesceExpression coalesceA && b is ActiveCoalesceExpression coalesceB
        ?
        coalesceA == coalesceB
        :
        a is ActiveOrElseExpression orElseA && b is ActiveOrElseExpression orElseB
        ?
        orElseA == orElseB
        :
        a is ActiveBinaryExpression binaryA && b is ActiveBinaryExpression binaryB
        ?
        binaryA == binaryB
        :
        a is ActiveConditionalExpression conditionalA && b is ActiveConditionalExpression conditionalB
        ?
        conditionalA == conditionalB
        :
        a is ActiveConstantExpression constantA && b is ActiveConstantExpression constantB
        ?
        constantA == constantB
        :
        a is ActiveInvocationExpression invocationA && b is ActiveInvocationExpression invocationB
        ?
        invocationA == invocationB
        :
        a is ActiveIndexExpression indexA && b is ActiveIndexExpression indexB
        ?
        indexA == indexB
        :
        a is ActiveMemberExpression memberA && b is ActiveMemberExpression memberB
        ?
        memberA == memberB
        :
        a is ActiveMemberInitExpression memberInitA && b is ActiveMemberInitExpression memberInitB
        ?
        memberInitA == memberInitB
        :
        a is ActiveMethodCallExpression methodCallA && b is ActiveMethodCallExpression methodCallB
        ?
        methodCallA == methodCallB
        :
        a is ActiveNewArrayInitExpression newArrayInitA && b is ActiveNewArrayInitExpression newArrayInitB
        ?
        newArrayInitA == newArrayInitB
        :
        a is ActiveNewExpression newA && b is ActiveNewExpression newB
        ?
        newA == newB
        :
        a is ActiveTypeBinaryExpression typeBinaryA && b is ActiveTypeBinaryExpression typeBinaryB
        ?
        typeBinaryA == typeBinaryB
        :
        a is ActiveUnaryExpression unaryA && b is ActiveUnaryExpression unaryB
        ?
        unaryA == unaryB
        :
        a is null && b is null;

    /// <summary>
    /// Determines whether two active expression tree nodes are different
    /// </summary>
    /// <param name="a">The first node to compare, or null</param>
    /// <param name="b">The second node to compare, or null</param>
    /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveExpression? a, ActiveExpression? b) =>
        !(a == b);
}

/// <summary>
/// Represents an active evaluation of a lambda expression
/// </summary>
/// <typeparam name="TResult">The type of the value returned by the lambda expression upon which this active expression is based</typeparam>
public sealed class ActiveExpression<TResult> :
    SyncDisposable,
    IActiveExpression<TResult>,
    IEquatable<ActiveExpression<TResult>>,
    IObserveActiveExpressions<object?>
{
    ActiveExpression(ActiveExpression activeExpression, ActiveExpressionOptions? options, EquatableList<object?> arguments)
    {
        this.activeExpression = activeExpression;
        Options = options;
        this.arguments = arguments;
        fault = this.activeExpression.Fault;
        val = this.activeExpression.Value is TResult value ? value : default;
        this.activeExpression.AddActiveExpressionOserver(this);
    }

    readonly ActiveExpression activeExpression;
    readonly EquatableList<object?> arguments;
    int disposalCount;
    Exception? fault;
    int? hashCode;
    readonly List<IObserveActiveExpressions<TResult>> observers = new();
    readonly object observersAccess = new();
    IReadOnlyList<IObserveActiveExpressions<TResult>> observersCopy = Array.Empty<IObserveActiveExpressions<TResult>>();
    bool observersCopyIsValid = true;
    TResult? val;

    /// <summary>
    /// Gets the arguments that were passed to the lambda expression
    /// </summary>
    public IReadOnlyList<object?> Arguments =>
        arguments;

    /// <summary>
    /// Gets the exception that was thrown while evaluating the lambda expression; <c>null</c> if there was no such exception
    /// </summary>
    public Exception? Fault
    {
        get => fault;
        private set => SetBackedProperty(ref fault, in value);
    }

    /// <summary>
    /// Gets the options used when creating the active expression
    /// </summary>
    public ActiveExpressionOptions? Options { get; }

    /// <summary>
    /// Gets the result of evaluating the lambda expression
    /// </summary>
    public TResult? Value
    {
        get => val;
        private set => SetBackedProperty(ref val, in value);
    }

    void IObserveActiveExpressions<object?>.ActiveExpressionChanged(IObservableActiveExpression<object?> activeExpression, object? oldValue, object? newValue, Exception? oldFault, Exception? newFault)
    {
        if (ReferenceEquals(activeExpression, this.activeExpression))
        {
            Fault = newFault;
            var myOldValue = val;
            var myNewValue = newValue is TResult typedValue ? typedValue : default;
            Value = myNewValue;
            lock (observersAccess)
            {
                if (!observersCopyIsValid)
                {
                    observersCopy = observers.ToImmutableArray();
                    observersCopyIsValid = true;
                }
            }
            for (int i = 0, ii = observersCopy.Count; i < ii; ++i)
                observersCopy[i].ActiveExpressionChanged(this, myOldValue, myNewValue, oldFault, newFault);
        }
    }

    /// <inheritdoc/>
    public void AddActiveExpressionOserver(IObserveActiveExpressions<TResult> observer)
    {
        lock (observersAccess)
        {
            observers.Add(observer);
            observersCopyIsValid = false;
        }
    }

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    /// <returns><c>true</c> if disposal completed; otherwise, <c>false</c></returns>
    protected override bool Dispose(bool disposing)
    {
        lock (instanceManagementLock)
        {
            if (--disposalCount > 0)
                return false;
            instances.Remove(new InstancesKey(activeExpression, arguments));
        }
        activeExpression.RemoveActiveExpressionObserver(this);
        activeExpression.Dispose();
        return true;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="obj">The object to compare with the current object</param>
    /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
    public override bool Equals(object? obj) =>
        obj is ActiveExpression<TResult> other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="ActiveExpression{TResult}"/> is equal to the current <see cref="ActiveExpression{TResult}"/>
    /// </summary>
    /// <param name="other">The other <see cref="ActiveExpression{TResult}"/></param>
    /// <returns><c>true</c> if the specified <see cref="ActiveExpression{TResult}"/> is equal to the current <see cref="ActiveExpression{TResult}"/>; otherwise, <c>false</c></returns>
    public bool Equals(ActiveExpression<TResult> other) =>
        other is null ? throw new ArgumentNullException(nameof(other)) : activeExpression == other.activeExpression;

    /// <summary>
    /// Gets the hash code for this active expression
    /// </summary>
    /// <returns>The hash code for this active expression</returns>
    public override int GetHashCode() =>
        hashCode ??= HashCode.Combine(typeof(ActiveExpression<TResult>), activeExpression);

    /// <inheritdoc/>
    public void RemoveActiveExpressionObserver(IObserveActiveExpressions<TResult> observer)
    {
        lock (observersAccess)
        {
            observers.Remove(observer);
            observersCopyIsValid = false;
        }
    }

    /// <summary>
    /// Returns a string that represents this active expression
    /// </summary>
    /// <returns>A string that represents this active expression</returns>
    public override string ToString() =>
        activeExpression.ToString();

    static readonly object instanceManagementLock = new();
    static readonly Dictionary<InstancesKey, ActiveExpression<TResult>> instances = new();

    internal static ActiveExpression<TResult> Create(LambdaExpression expression, ActiveExpressionOptions? options, params object?[] args)
    {
        var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, args), options, false);
        var arguments = new EquatableList<object?>(args);
        var key = new InstancesKey(activeExpression, arguments);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var instance))
            {
                instance = new ActiveExpression<TResult>(activeExpression, options, arguments);
                instances.Add(key, instance);
            }
            ++instance.disposalCount;
            return instance;
        }
    }

    /// <summary>
    /// Determines whether two active expressions are the same
    /// </summary>
    /// <param name="a">The first expression to compare, or null</param>
    /// <param name="b">The second expression to compare, or null</param>
    /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
    public static bool operator ==(ActiveExpression<TResult> a, ActiveExpression<TResult> b) =>
        a is null ? throw new ArgumentNullException(nameof(a)) : a.Equals(b);

    /// <summary>
    /// Determines whether two active expressions are different
    /// </summary>
    /// <param name="a">The first expression to compare, or null</param>
    /// <param name="b">The second expression to compare, or null</param>
    /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
    public static bool operator !=(ActiveExpression<TResult> a, ActiveExpression<TResult> b) =>
        !(a == b);

    sealed record InstancesKey(ActiveExpression ActiveExpression, EquatableList<object?> Args);
}

/// <summary>
/// Represents an active evaluation of a strongly-typed lambda expression with a single argument
/// </summary>
/// <typeparam name="TArg">The type of the argument passed to the lambda expression</typeparam>
/// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
public sealed class ActiveExpression<TArg, TResult> :
    SyncDisposable,
    IActiveExpression<TArg, TResult>,
    IEquatable<ActiveExpression<TArg, TResult>>,
    IObserveActiveExpressions<object?>
{
    ActiveExpression(ActiveExpression activeExpression, ActiveExpressionOptions? options, TArg arg)
    {
        this.activeExpression = activeExpression;
        Options = options;
        arguments = new EquatableList<object?>(new object?[] { arg });
        Arg = arg;
        fault = this.activeExpression.Fault;
        val = this.activeExpression.Value is TResult value ? value : default;
        this.activeExpression.AddActiveExpressionOserver(this);
    }

    readonly ActiveExpression activeExpression;
    readonly EquatableList<object?> arguments;
    int disposalCount;
    Exception? fault;
    int? hashCode;
    readonly List<IObserveActiveExpressions<TResult>> observers = new();
    readonly object observersAccess = new();
    IReadOnlyList<IObserveActiveExpressions<TResult>> observersCopy = Array.Empty<IObserveActiveExpressions<TResult>>();
    bool observersCopyIsValid = true;
    TResult? val;

    /// <summary>
    /// Gets the argument that was passed to the lambda expression
    /// </summary>
    public TArg Arg { get; }

    /// <summary>
    /// Gets the arguments that were passed to the lambda expression
    /// </summary>
    public IReadOnlyList<object?> Arguments =>
        arguments;

    /// <summary>
    /// Gets the exception that was thrown while evaluating the lambda expression; <c>null</c> if there was no such exception
    /// </summary>
    public Exception? Fault
    {
        get => fault;
        private set => SetBackedProperty(ref fault, in value);
    }

    /// <summary>
    /// Gets the options used when creating the active expression
    /// </summary>
    public ActiveExpressionOptions? Options { get; }

    /// <summary>
    /// Gets the result of evaluating the lambda expression
    /// </summary>
    public TResult? Value
    {
        get => val;
        private set => SetBackedProperty(ref val, in value);
    }

    void IObserveActiveExpressions<object?>.ActiveExpressionChanged(IObservableActiveExpression<object?> activeExpression, object? oldValue, object? newValue, Exception? oldFault, Exception? newFault)
    {
        if (ReferenceEquals(activeExpression, this.activeExpression))
        {
            Fault = newFault;
            var myOldValue = val;
            var myNewValue = newValue is TResult typedValue ? typedValue : default;
            Value = myNewValue;
            lock (observersAccess)
            {
                if (!observersCopyIsValid)
                {
                    observersCopy = observers.ToImmutableArray();
                    observersCopyIsValid = true;
                }
            }
            for (int i = 0, ii = observersCopy.Count; i < ii; ++i)
                observersCopy[i].ActiveExpressionChanged(this, myOldValue, myNewValue, oldFault, newFault);
        }
    }

    /// <inheritdoc/>
    public void AddActiveExpressionOserver(IObserveActiveExpressions<TResult> observer)
    {
        lock (observersAccess)
        {
            observers.Add(observer);
            observersCopyIsValid = false;
        }
    }

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    /// <returns><c>true</c> if disposal completed; otherwise, <c>false</c></returns>
    protected override bool Dispose(bool disposing)
    {
        lock (instanceManagementLock)
        {
            if (--disposalCount > 0)
                return false;
            instances.Remove(new InstancesKey(activeExpression, Arg));
        }
        activeExpression.RemoveActiveExpressionObserver(this);
        activeExpression.Dispose();
        return true;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="obj">The object to compare with the current object</param>
    /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
    public override bool Equals(object? obj) =>
        obj is ActiveExpression<TArg, TResult> other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="ActiveExpression{TArg, TResult}"/> is equal to the current <see cref="ActiveExpression{TArg, TResult}"/>
    /// </summary>
    /// <param name="other">The <see cref="ActiveExpression{TArg, TResult}"/> to compare with the current <see cref="ActiveExpression{TArg, TResult}"/></param>
    /// <returns><c>true</c> if the specified <see cref="ActiveExpression{TArg, TResult}"/> is equal to the current <see cref="ActiveExpression{TArg, TResult}"/>; otherwise, <c>false</c></returns>
    public bool Equals(ActiveExpression<TArg, TResult> other) =>
        other is null ? throw new ArgumentNullException(nameof(other)) : activeExpression == other.activeExpression;

    /// <summary>
    /// Gets the hash code for this active expression
    /// </summary>
    /// <returns>The hash code for this active expression</returns>
    public override int GetHashCode() =>
        hashCode ??= HashCode.Combine(typeof(ActiveExpression<TArg, TResult>), activeExpression);

    /// <inheritdoc/>
    public void RemoveActiveExpressionObserver(IObserveActiveExpressions<TResult> observer)
    {
        lock (observersAccess)
        {
            observers.Remove(observer);
            observersCopyIsValid = false;
        }
    }

    /// <summary>
    /// Returns a string that represents this active expression
    /// </summary>
    /// <returns>A string that represents this active expression</returns>
    public override string ToString() =>
        activeExpression.ToString();

    static readonly object instanceManagementLock = new();
    static readonly Dictionary<InstancesKey, ActiveExpression<TArg, TResult>> instances = new();

    internal static ActiveExpression<TArg, TResult> Create(LambdaExpression expression, TArg arg, ActiveExpressionOptions? options = null)
    {
        var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg), options, false);
        var key = new InstancesKey(activeExpression, arg);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var instance))
            {
                instance = new ActiveExpression<TArg, TResult>(activeExpression, options, arg);
                instances.Add(key, instance);
            }
            ++instance.disposalCount;
            return instance;
        }
    }

    /// <summary>
    /// Determines whether two active expressions are the same
    /// </summary>
    /// <param name="a">The first expression to compare, or null</param>
    /// <param name="b">The second expression to compare, or null</param>
    /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
    public static bool operator ==(ActiveExpression<TArg, TResult> a, ActiveExpression<TArg, TResult> b) =>
        a is null ? throw new ArgumentNullException(nameof(a)) : a.Equals(b);

    /// <summary>
    /// Determines whether two active expressions are different
    /// </summary>
    /// <param name="a">The first expression to compare, or null</param>
    /// <param name="b">The second expression to compare, or null</param>
    /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
    public static bool operator !=(ActiveExpression<TArg, TResult> a, ActiveExpression<TArg, TResult> b) =>
        !(a == b);

    sealed record InstancesKey(ActiveExpression ActiveExpression, TArg Arg);
}

/// <summary>
/// Represents an active evaluation of a strongly-typed lambda expression with two arguments
/// </summary>
/// <typeparam name="TArg1">The type of the first argument passed to the lambda expression</typeparam>
/// <typeparam name="TArg2">The type of the second argument passed to the lambda expression</typeparam>
/// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
[SuppressMessage("Code Analysis", "CA1005: Avoid excessive parameters on generic types")]
public sealed class ActiveExpression<TArg1, TArg2, TResult> :
    SyncDisposable,
    IActiveExpression<TArg1, TArg2, TResult>,
    IEquatable<ActiveExpression<TArg1, TArg2, TResult>>,
    IObserveActiveExpressions<object?>
{
    ActiveExpression(ActiveExpression activeExpression, ActiveExpressionOptions? options, TArg1 arg1, TArg2 arg2)
    {
        this.activeExpression = activeExpression;
        Options = options;
        arguments = new EquatableList<object?>(new object?[] { arg1, arg2 });
        Arg1 = arg1;
        Arg2 = arg2;
        fault = this.activeExpression.Fault;
        val = this.activeExpression.Value is TResult value ? value : default;
        this.activeExpression.AddActiveExpressionOserver(this);
    }

    readonly ActiveExpression activeExpression;
    readonly EquatableList<object?> arguments;
    int disposalCount;
    Exception? fault;
    int? hashCode;
    readonly List<IObserveActiveExpressions<TResult>> observers = new();
    readonly object observersAccess = new();
    IReadOnlyList<IObserveActiveExpressions<TResult>> observersCopy = Array.Empty<IObserveActiveExpressions<TResult>>();
    bool observersCopyIsValid = true;
    TResult? val;

    /// <summary>
    /// Gets the arguments that were passed to the lambda expression
    /// </summary>
    public IReadOnlyList<object?> Arguments =>
        arguments;

    /// <summary>
    /// Gets the first argument that was passed to the lambda expression
    /// </summary>
    public TArg1 Arg1 { get; }

    /// <summary>
    /// Gets the second argument that was passed to the lambda expression
    /// </summary>
    public TArg2 Arg2 { get; }

    /// <summary>
    /// Gets the exception that was thrown while evaluating the lambda expression; <c>null</c> if there was no such exception
    /// </summary>
    public Exception? Fault
    {
        get => fault;
        private set => SetBackedProperty(ref fault, in value);
    }

    /// <summary>
    /// Gets the options used when creating the active expression
    /// </summary>
    public ActiveExpressionOptions? Options { get; }

    /// <summary>
    /// Gets the result of evaluating the lambda expression
    /// </summary>
    public TResult? Value
    {
        get => val;
        private set => SetBackedProperty(ref val, in value);
    }

    void IObserveActiveExpressions<object?>.ActiveExpressionChanged(IObservableActiveExpression<object?> activeExpression, object? oldValue, object? newValue, Exception? oldFault, Exception? newFault)
    {
        if (ReferenceEquals(activeExpression, this.activeExpression))
        {
            Fault = newFault;
            var myOldValue = val;
            var myNewValue = newValue is TResult typedValue ? typedValue : default;
            Value = myNewValue;
            lock (observersAccess)
            {
                if (!observersCopyIsValid)
                {
                    observersCopy = observers.ToImmutableArray();
                    observersCopyIsValid = true;
                }
            }
            for (int i = 0, ii = observersCopy.Count; i < ii; ++i)
                observersCopy[i].ActiveExpressionChanged(this, myOldValue, myNewValue, oldFault, newFault);
        }
    }

    /// <inheritdoc/>
    public void AddActiveExpressionOserver(IObserveActiveExpressions<TResult> observer)
    {
        lock (observersAccess)
        {
            observers.Add(observer);
            observersCopyIsValid = false;
        }
    }

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    /// <returns><c>true</c> if disposal completed; otherwise, <c>false</c></returns>
    protected override bool Dispose(bool disposing)
    {
        lock (instanceManagementLock)
        {
            if (--disposalCount > 0)
                return false;
            instances.Remove(new InstancesKey(activeExpression, Arg1, Arg2));
        }
        activeExpression.RemoveActiveExpressionObserver(this);
        activeExpression.Dispose();
        return true;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="obj">The object to compare with the current object</param>
    /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
    public override bool Equals(object? obj) =>
        obj is ActiveExpression<TArg1, TArg2, TResult> other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="ActiveExpression{TArg1, TArg2, TResult}"/> is equal to the current object
    /// </summary>
    /// <param name="other">The object to compare with the current object</param>
    /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
    public bool Equals(ActiveExpression<TArg1, TArg2, TResult> other) =>
        other is null ? throw new ArgumentNullException(nameof(other)) : activeExpression == other.activeExpression;

    /// <summary>
    /// Gets the hash code for this active expression
    /// </summary>
    /// <returns>The hash code for this active expression</returns>
    public override int GetHashCode() =>
        hashCode ??= HashCode.Combine(typeof(ActiveExpression<TArg1, TArg2, TResult>), activeExpression);

    /// <inheritdoc/>
    public void RemoveActiveExpressionObserver(IObserveActiveExpressions<TResult> observer)
    {
        lock (observersAccess)
        {
            observers.Remove(observer);
            observersCopyIsValid = false;
        }
    }

    /// <summary>
    /// Returns a string that represents this active expression
    /// </summary>
    /// <returns>A string that represents this active expression</returns>
    public override string ToString() =>
        activeExpression.ToString();

    static readonly object instanceManagementLock = new();
    static readonly Dictionary<InstancesKey, ActiveExpression<TArg1, TArg2, TResult>> instances = new();

    internal static ActiveExpression<TArg1, TArg2, TResult> Create(LambdaExpression expression, TArg1 arg1, TArg2 arg2, ActiveExpressionOptions? options = null)
    {
        var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg1, arg2), options, false);
        var key = new InstancesKey(activeExpression, arg1, arg2);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var instance))
            {
                instance = new ActiveExpression<TArg1, TArg2, TResult>(activeExpression, options, arg1, arg2);
                instances.Add(key, instance);
            }
            ++instance.disposalCount;
            return instance;
        }
    }

    /// <summary>
    /// Determines whether two active expressions are the same
    /// </summary>
    /// <param name="a">The first expression to compare, or null</param>
    /// <param name="b">The second expression to compare, or null</param>
    /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
    public static bool operator ==(ActiveExpression<TArg1, TArg2, TResult> a, ActiveExpression<TArg1, TArg2, TResult> b) =>
        a is null ? throw new ArgumentNullException(nameof(a)) : a.Equals(b);

    /// <summary>
    /// Determines whether two active expressions are different
    /// </summary>
    /// <param name="a">The first expression to compare, or null</param>
    /// <param name="b">The second expression to compare, or null</param>
    /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
    public static bool operator !=(ActiveExpression<TArg1, TArg2, TResult> a, ActiveExpression<TArg1, TArg2, TResult> b) =>
        !(a == b);

    sealed record InstancesKey(ActiveExpression ActiveExpression, TArg1 Arg1, TArg2 Arg2);
}

/// <summary>
/// Represents an active evaluation of a strongly-typed lambda expression with three arguments
/// </summary>
/// <typeparam name="TArg1">The type of the first argument passed to the lambda expression</typeparam>
/// <typeparam name="TArg2">The type of the second argument passed to the lambda expression</typeparam>
/// <typeparam name="TArg3">The type of the third argument passed to the lambda expression</typeparam>
/// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
[SuppressMessage("Code Analysis", "CA1005: Avoid excessive parameters on generic types")]
public sealed class ActiveExpression<TArg1, TArg2, TArg3, TResult> :
    SyncDisposable,
    IActiveExpression<TArg1, TArg2, TArg3, TResult>,
    IEquatable<ActiveExpression<TArg1, TArg2, TArg3, TResult>>,
    IObserveActiveExpressions<object?>
{
    ActiveExpression(ActiveExpression activeExpression, ActiveExpressionOptions? options, TArg1 arg1, TArg2 arg2, TArg3 arg3)
    {
        this.activeExpression = activeExpression;
        Options = options;
        arguments = new EquatableList<object?>(new object?[] { arg1, arg2, arg3 });
        Arg1 = arg1;
        Arg2 = arg2;
        Arg3 = arg3;
        fault = this.activeExpression.Fault;
        val = this.activeExpression.Value is TResult value ? value : default;
        this.activeExpression.AddActiveExpressionOserver(this);
    }

    readonly ActiveExpression activeExpression;
    readonly EquatableList<object?> arguments;
    int disposalCount;
    Exception? fault;
    int? hashCode;
    readonly List<IObserveActiveExpressions<TResult>> observers = new();
    readonly object observersAccess = new();
    IReadOnlyList<IObserveActiveExpressions<TResult>> observersCopy = Array.Empty<IObserveActiveExpressions<TResult>>();
    bool observersCopyIsValid = true;
    TResult? val;

    /// <summary>
    /// Gets the arguments that were passed to the lambda expression
    /// </summary>
    public IReadOnlyList<object?> Arguments =>
        arguments;

    /// <summary>
    /// Gets the first argument that was passed to the lambda expression
    /// </summary>
    public TArg1 Arg1 { get; }

    /// <summary>
    /// Gets the second argument that was passed to the lambda expression
    /// </summary>
    public TArg2 Arg2 { get; }

    /// <summary>
    /// Gets the third argument that was passed to the lambda expression
    /// </summary>
    public TArg3 Arg3 { get; }

    /// <summary>
    /// Gets the exception that was thrown while evaluating the lambda expression; <c>null</c> if there was no such exception
    /// </summary>
    public Exception? Fault
    {
        get => fault;
        private set => SetBackedProperty(ref fault, in value);
    }

    /// <summary>
    /// Gets the options used when creating the active expression
    /// </summary>
    public ActiveExpressionOptions? Options { get; }

    /// <summary>
    /// Gets the result of evaluating the lambda expression
    /// </summary>
    public TResult? Value
    {
        get => val;
        private set => SetBackedProperty(ref val, in value);
    }

    void IObserveActiveExpressions<object?>.ActiveExpressionChanged(IObservableActiveExpression<object?> activeExpression, object? oldValue, object? newValue, Exception? oldFault, Exception? newFault)
    {
        if (ReferenceEquals(activeExpression, this.activeExpression))
        {
            Fault = newFault;
            var myOldValue = val;
            var myNewValue = newValue is TResult typedValue ? typedValue : default;
            Value = myNewValue;
            lock (observersAccess)
            {
                if (!observersCopyIsValid)
                {
                    observersCopy = observers.ToImmutableArray();
                    observersCopyIsValid = true;
                }
            }
            for (int i = 0, ii = observersCopy.Count; i < ii; ++i)
                observersCopy[i].ActiveExpressionChanged(this, myOldValue, myNewValue, oldFault, newFault);
        }
    }

    /// <inheritdoc/>
    public void AddActiveExpressionOserver(IObserveActiveExpressions<TResult> observer)
    {
        lock (observersAccess)
        {
            observers.Add(observer);
            observersCopyIsValid = false;
        }
    }

    /// <summary>
    /// Frees, releases, or resets unmanaged resources
    /// </summary>
    /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
    /// <returns><c>true</c> if disposal completed; otherwise, <c>false</c></returns>
    protected override bool Dispose(bool disposing)
    {
        lock (instanceManagementLock)
        {
            if (--disposalCount > 0)
                return false;
            instances.Remove(new InstancesKey(activeExpression, Arg1, Arg2, Arg3));
        }
        activeExpression.RemoveActiveExpressionObserver(this);
        activeExpression.Dispose();
        return true;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="obj">The object to compare with the current object</param>
    /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
    public override bool Equals(object? obj) =>
        obj is ActiveExpression<TArg1, TArg2, TArg3, TResult> other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/> is equal to the current <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/>
    /// </summary>
    /// <param name="other">The <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/> to compare with the current <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/></param>
    /// <returns><c>true</c> if the specified <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/> is equal to the current <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/>; otherwise, <c>false</c></returns>
    public bool Equals(ActiveExpression<TArg1, TArg2, TArg3, TResult> other) =>
        other is null ? throw new ArgumentNullException(nameof(other)) : activeExpression == other.activeExpression;

    /// <summary>
    /// Gets the hash code for this active expression
    /// </summary>
    /// <returns>The hash code for this active expression</returns>
    public override int GetHashCode() =>
        hashCode ??= HashCode.Combine(typeof(ActiveExpression<TArg1, TArg2, TArg3, TResult>), activeExpression);

    /// <inheritdoc/>
    public void RemoveActiveExpressionObserver(IObserveActiveExpressions<TResult> observer)
    {
        lock (observersAccess)
        {
            observers.Remove(observer);
            observersCopyIsValid = false;
        }
    }

    /// <summary>
    /// Returns a string that represents this active expression
    /// </summary>
    /// <returns>A string that represents this active expression</returns>
    public override string ToString() =>
        activeExpression.ToString();

    static readonly object instanceManagementLock = new();
    static readonly Dictionary<InstancesKey, ActiveExpression<TArg1, TArg2, TArg3, TResult>> instances = new();

    internal static ActiveExpression<TArg1, TArg2, TArg3, TResult> Create(LambdaExpression expression, TArg1 arg1, TArg2 arg2, TArg3 arg3, ActiveExpressionOptions? options = null)
    {
        var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg1, arg2, arg3), options, false);
        var key = new InstancesKey(activeExpression, arg1, arg2, arg3);
        lock (instanceManagementLock)
        {
            if (!instances.TryGetValue(key, out var instance))
            {
                instance = new ActiveExpression<TArg1, TArg2, TArg3, TResult>(activeExpression, options, arg1, arg2, arg3);
                instances.Add(key, instance);
            }
            ++instance.disposalCount;
            return instance;
        }
    }

    /// <summary>
    /// Determines whether two active expressions are the same
    /// </summary>
    /// <param name="a">The first expression to compare, or null</param>
    /// <param name="b">The second expression to compare, or null</param>
    /// <returns><c>true</c> is <paramref name="a"/> is the same as <paramref name="b"/>; otherwise, <c>false</c></returns>
    public static bool operator ==(ActiveExpression<TArg1, TArg2, TArg3, TResult> a, ActiveExpression<TArg1, TArg2, TArg3, TResult> b) =>
        a is null ? throw new ArgumentNullException(nameof(a)) : a.Equals(b);

    /// <summary>
    /// Determines whether two active expressions are different
    /// </summary>
    /// <param name="a">The first expression to compare, or null</param>
    /// <param name="b">The second expression to compare, or null</param>
    /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
    public static bool operator !=(ActiveExpression<TArg1, TArg2, TArg3, TResult> a, ActiveExpression<TArg1, TArg2, TArg3, TResult> b) =>
        !(a == b);

    sealed record InstancesKey(ActiveExpression ActiveExpression, TArg1 Arg1, TArg2 Arg2, TArg3 Arg3);
}
