namespace Cogs.Reflection;

/// <summary>
/// Provides a method for invoking a method that is not known at compile time
/// </summary>
public sealed class FastMethodInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FastMethodInfo"/> class based on the specified <see cref="System.Reflection.MethodInfo"/>
    /// </summary>
    /// <param name="methodInfo">The <see cref="System.Reflection.MethodInfo"/> reflecting the method to be invoked</param>
    FastMethodInfo(MethodInfo methodInfo)
    {
        if (methodInfo is null)
            throw new ArgumentNullException(nameof(methodInfo));
        MethodInfo = methodInfo;
        if (compileExpressionTrees)
        {
            var instanceExpression = Expression.Parameter(typeof(object), "instance");
            var argumentsExpression = Expression.Parameter(typeof(object[]), "arguments");
            var argumentExpressions = new List<Expression>();
            var parameterInfos = methodInfo.GetParameters();
            for (var i = 0; i < parameterInfos.Length; ++i)
            {
                var parameterInfo = parameterInfos[i];
                argumentExpressions.Add(Expression.Convert(Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)), parameterInfo.ParameterType));
            }
            var callExpression = Expression.Call(!methodInfo.IsStatic ? Expression.Convert(instanceExpression, methodInfo.DeclaringType) : null, methodInfo, argumentExpressions);
            if (callExpression.Type == typeof(void))
            {
                var voidDelegate = Expression.Lambda<VoidDelegate>(callExpression, instanceExpression, argumentsExpression).Compile();
                @delegate = (instance, arguments) =>
                {
                    voidDelegate(instance, arguments);
                    return null;
                };
            }
            else
                @delegate = Expression.Lambda<ReturnValueDelegate>(Expression.Convert(callExpression, typeof(object)), instanceExpression, argumentsExpression).Compile();
        }
    }

    readonly ReturnValueDelegate? @delegate;

    /// <summary>
    /// Invokes the method reflected by <see cref="MethodInfo"/>
    /// </summary>
    /// <param name="instance">The object on which to invoke the method (if a method is static, this argument is ignored)</param>
    /// <param name="arguments">An argument list for the invoked method</param>
    /// <returns>An object containing the return value of the invoked method</returns>
    public object? Invoke(object? instance, params object?[] arguments) => compileExpressionTrees ? @delegate!(instance, arguments) : MethodInfo.Invoke(instance, arguments);

    /// <summary>
    /// Gets the <see cref="System.Reflection.MethodInfo"/> reflecting the method this <see cref="FastMethodInfo"/> will invoke
    /// </summary>
    public MethodInfo MethodInfo { get; }

    static readonly bool compileExpressionTrees = Environment.Version.Major <= 6;
    static readonly ConcurrentDictionary<MethodInfo, FastMethodInfo> fastMethodInfos = new();

    static FastMethodInfo Create(MethodInfo methodInfo) => new(methodInfo);

    /// <summary>
    /// Get a <see cref="FastMethodInfo"/> for the specified <see cref="System.Reflection.MethodInfo" />
    /// </summary>
    /// <param name="methodInfo">The <see cref="System.Reflection.MethodInfo"/></param>
    /// <returns>A <see cref="FastMethodInfo"/></returns>
    public static FastMethodInfo Get(MethodInfo methodInfo) =>
        methodInfo is null ? throw new ArgumentNullException(nameof(methodInfo)) : fastMethodInfos.GetOrAdd(methodInfo, Create);

    delegate object? ReturnValueDelegate(object? instance, object?[] arguments);
    delegate void VoidDelegate(object? instance, object?[] arguments);
}
