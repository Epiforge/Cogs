namespace Cogs.Reflection;

/// <summary>
/// Provides a method for invoking a constructor that is not known at compile time
/// </summary>
public sealed class FastConstructorInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FastConstructorInfo"/> class based on the specified <see cref="System.Reflection.ConstructorInfo"/>
    /// </summary>
    /// <param name="constructorInfo">The <see cref="System.Reflection.ConstructorInfo"/> reflecting the constructor to be invoked</param>
    FastConstructorInfo(ConstructorInfo constructorInfo)
    {
        if (constructorInfo is null)
            throw new ArgumentNullException(nameof(constructorInfo));
        ConstructorInfo = constructorInfo;
        if (compileExpressionTrees)
        {
            var argumentsExpression = Expression.Parameter(typeof(object[]), "arguments");
            var argumentExpressions = new List<Expression>();
            var parameterInfos = constructorInfo.GetParameters();
            for (var i = 0; i < parameterInfos.Length; ++i)
            {
                var parameterInfo = parameterInfos[i];
                argumentExpressions.Add(Expression.Convert(Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)), parameterInfo.ParameterType));
            }
            @delegate = Expression.Lambda<ConstructorDelegate>(Expression.Convert(Expression.New(constructorInfo, argumentExpressions), typeof(object)), argumentsExpression).Compile();
        }
    }

    readonly ConstructorDelegate? @delegate;

    /// <summary>
    /// Invokes the constructor reflected by <see cref="ConstructorInfo"/>
    /// </summary>
    /// <param name="arguments">An argument list for the invoked constructor</param>
    /// <returns>The constructed object</returns>
    public object Invoke(params object?[] arguments) => compileExpressionTrees ? @delegate!(arguments) : ConstructorInfo.Invoke(arguments);

    /// <summary>
    /// Gets the <see cref="System.Reflection.ConstructorInfo"/> reflecting the method this <see cref="FastConstructorInfo"/> will invoke
    /// </summary>
    public ConstructorInfo ConstructorInfo { get; }

    static readonly bool compileExpressionTrees = Environment.Version.Major <= 6;
    static readonly ConcurrentDictionary<ConstructorInfo, FastConstructorInfo> fastConstructorInfos = new();

    static FastConstructorInfo Create(ConstructorInfo constructorInfo) => new(constructorInfo);

    /// <summary>
    /// Get a <see cref="FastConstructorInfo"/> for the specified <see cref="System.Reflection.ConstructorInfo" />
    /// </summary>
    /// <param name="constructorInfo">The <see cref="System.Reflection.ConstructorInfo"/></param>
    /// <returns>A <see cref="FastConstructorInfo"/></returns>
    public static FastConstructorInfo Get(ConstructorInfo constructorInfo) =>
        constructorInfo is null ? throw new ArgumentNullException(nameof(constructorInfo)) : fastConstructorInfos.GetOrAdd(constructorInfo, Create);

    delegate object ConstructorDelegate(object?[] arguments);
}
