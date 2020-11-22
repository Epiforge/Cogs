using Cogs.Collections;
using Cogs.Disposal;
using Cogs.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Cogs.ActiveExpressions
{
    /// <summary>
    /// Provides the base class from which the classes that represent active expression tree nodes are derived; use <see cref="Create{TResult}(LambdaExpression, object[])"/> or one of its overloads to create an active expression
    /// </summary>
    public abstract class ActiveExpression : SyncDisposable
    {
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
        protected ActiveExpression(Type type, ExpressionType nodeType, ActiveExpressionOptions? options, object value) : this(type, nodeType, options, false) => val = value;

        readonly object? defaultValue;
        bool deferringEvaluation;
        readonly object deferringEvaluationLock = new object();
        Exception? fault;
        object? val;
        readonly FastEqualityComparer valueEqualityComparer;

        /// <summary>
        /// The <see cref="ActiveExpressionOptions"/> instance for this node
        /// </summary>
        protected readonly ActiveExpressionOptions? options;

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
            protected set
            {
                SetBackedProperty(ref val, in defaultValue, nameof(Value));
                SetBackedProperty(ref fault, in value);
            }
        }

        /// <summary>
        /// Gets whether evaluation is being deferred
        /// </summary>
        protected bool IsDeferringEvaluation
        {
            get
            {
                lock (deferringEvaluationLock)
                    return deferringEvaluation;
            }
        }

        /// <summary>
        /// Gets the <see cref="ExpressionType"/> for this node
        /// </summary>
        public ExpressionType NodeType { get; }

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
            protected set
            {
                SetBackedProperty(ref fault, null, nameof(Fault));
                if (!valueEqualityComparer.Equals(value, val))
                {
                    var previousValue = val;
                    OnPropertyChanging();
                    val = value;
                    OnPropertyChanged();
                    DisposeIfNecessaryAndPossible(previousValue);
                }
            }
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
                        ThreadPool.QueueUserWorkItem(async state => await asyncDisposable.DisposeAsync());
                }
                else if (value is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        /// <summary>
        /// Disposes of the expression's value if necessary and possible (intended to be called within <see cref="SyncDisposable.Dispose(bool)"/>)
        /// </summary>
        protected void DisposeValueIfNecessaryAndPossible() => DisposeIfNecessaryAndPossible(val);

        /// <summary>
        /// Throws a <see cref="NotImplementedException"/> because deriving classes should be overriding this method
        /// </summary>
        /// <param name="obj">The object to compare with the current object</param>
        public override bool Equals(object? obj) => throw new NotImplementedException();

        /// <summary>
        /// Evaluates the current node
        /// </summary>
        protected virtual void Evaluate()
        {
        }

        void EvaluateIfDeferred()
        {
            lock (deferringEvaluationLock)
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
            lock (deferringEvaluationLock)
            {
                if (!deferringEvaluation)
                    Evaluate();
            }
        }

        /// <summary>
        /// Throws a <see cref="NotImplementedException"/> because deriving classes should be overriding this method
        /// </summary>
        public override int GetHashCode() => throw new NotImplementedException();

        /// <summary>
        /// Gets whether a value produced by this expression should be disposed
        /// </summary>
        /// <returns><c>true</c> if values from this expression should be disposed; otherwise, <c>false</c></returns>
        protected virtual bool GetShouldValueBeDisposed() => false;

        /// <summary>
        /// Attempts to get this node's value if its evaluation is not deferred
        /// </summary>
        /// <param name="value">The value if evaluation is not deferred</param>
        /// <returns><c>true</c> if evaluation is not deferred and <paramref name="value"/> has been set to the value; otherwise, <c>false</c></returns>
        protected bool TryGetUndeferredValue(out object? value)
        {
            lock (deferringEvaluationLock)
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

        /// <summary>
        /// Gets the currently applicable instance of <see cref="ActiveExpressionOptions"/> for this node
        /// </summary>
        protected ActiveExpressionOptions ApplicableOptions => options ?? ActiveExpressionOptions.Default;

        /// <summary>
        /// Gets the suffix of the string representation of this node
        /// </summary>
        protected string ToStringSuffix => $"/* {GetValueString(fault, !TryGetUndeferredValue(out var value), value)} */";

        static readonly ConcurrentDictionary<MethodInfo, PropertyInfo> propertyGetMethodToProperty = new ConcurrentDictionary<MethodInfo, PropertyInfo>(); // NCrunch: no coverage

        internal static ActiveExpression Create(Expression? expression, ActiveExpressionOptions? options, bool deferEvaluation)
        {
            ActiveExpression activeExpression;
            switch (expression)
            {
                case BinaryExpression binaryExpression:
                    activeExpression = ActiveBinaryExpression.Create(binaryExpression, options, deferEvaluation);
                    break;
                case ConditionalExpression conditionalExpression:
                    activeExpression = ActiveConditionalExpression.Create(conditionalExpression, options, deferEvaluation);
                    break;
                case ConstantExpression constantExpression:
                    activeExpression = ActiveConstantExpression.Create(constantExpression, options);
                    break;
                case InvocationExpression invocationExpression:
                    activeExpression = ActiveInvocationExpression.Create(invocationExpression, options, deferEvaluation);
                    break;
                case IndexExpression indexExpression:
                    activeExpression = ActiveIndexExpression.Create(indexExpression, options, deferEvaluation);
                    break;
                case MemberExpression memberExpression:
                    activeExpression = ActiveMemberExpression.Create(memberExpression, options, deferEvaluation);
                    break;
                case MethodCallExpression methodCallExpressionForPropertyGet when propertyGetMethodToProperty.GetOrAdd(methodCallExpressionForPropertyGet.Method, GetPropertyFromGetMethod) is PropertyInfo property:
                    if (methodCallExpressionForPropertyGet.Arguments.Count > 0)
                        activeExpression = ActiveIndexExpression.Create(Expression.MakeIndex(methodCallExpressionForPropertyGet.Object, property, methodCallExpressionForPropertyGet.Arguments), options, deferEvaluation);
                    else
                        activeExpression = ActiveMemberExpression.Create(Expression.MakeMemberAccess(methodCallExpressionForPropertyGet.Object, property), options, deferEvaluation);
                    break;
                case MethodCallExpression methodCallExpression:
                    activeExpression = ActiveMethodCallExpression.Create(methodCallExpression, options, deferEvaluation);
                    break;
                case NewArrayExpression newArrayInitExpression when newArrayInitExpression.NodeType == ExpressionType.NewArrayInit:
                    activeExpression = ActiveNewArrayInitExpression.Create(newArrayInitExpression, options, deferEvaluation);
                    break;
                case NewExpression newExpression:
                    activeExpression = ActiveNewExpression.Create(newExpression, options, deferEvaluation);
                    break;
                case TypeBinaryExpression typeBinaryExpression:
                    activeExpression = ActiveTypeBinaryExpression.Create(typeBinaryExpression, options, deferEvaluation);
                    break;
                case UnaryExpression unaryExpression when unaryExpression.NodeType == ExpressionType.Quote:
                    activeExpression = ActiveConstantExpression.Create(Expression.Constant(unaryExpression.Operand), options);
                    break;
                case UnaryExpression unaryExpression:
                    activeExpression = ActiveUnaryExpression.Create(unaryExpression, options, deferEvaluation);
                    break;
                default:
                    throw new NotSupportedException();
            }
            if (!deferEvaluation)
                activeExpression.EvaluateIfDeferred();
            return activeExpression;
        }

        /// <summary>
        /// Produces a human-readable representation of an expression
        /// </summary>
        /// <param name="expressionType">The type of expression</param>
        /// <param name="resultType">The type of the result when the expression is evaluated</param>
        /// <param name="operands">The operands (or arguments) of the expression</param>
        /// <returns>A human-readable representation of the expression</returns>
        public static string GetOperatorExpressionSyntax(ExpressionType expressionType, Type resultType, params object[] operands)
        {
            return expressionType switch
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
                ExpressionType.Not when operands[0] is bool || (operands[0] is ActiveExpression notOperand && (notOperand.Type == typeof(bool) || notOperand.Type == typeof(bool?))) => $"(!{operands[0]})",
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
        }

        static PropertyInfo GetPropertyFromGetMethod(MethodInfo getMethod) => getMethod.DeclaringType.GetRuntimeProperties().FirstOrDefault(property => property.GetMethod == getMethod);

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
        public static ActiveExpression<TArg?, TResult> Create<TArg, TResult>(Expression<Func<TArg?, TResult>> expression, TArg? arg, ActiveExpressionOptions? options = null)
        {
            options?.Freeze();
            return ActiveExpression<TArg?, TResult>.Create(expression, arg, options);
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
        public static ActiveExpression<TArg1?, TArg2?, TResult> Create<TArg1, TArg2, TResult>(Expression<Func<TArg1?, TArg2?, TResult>> expression, TArg1? arg1, TArg2? arg2, ActiveExpressionOptions? options = null)
        {
            options?.Freeze();
            return ActiveExpression<TArg1?, TArg2?, TResult>.Create(expression, arg1, arg2, options);
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
        public static ActiveExpression<TArg1?, TArg2?, TArg3?, TResult> Create<TArg1, TArg2, TArg3, TResult>(Expression<Func<TArg1?, TArg2?, TArg3?, TResult>> expression, TArg1? arg1, TArg2? arg2, TArg3? arg3, ActiveExpressionOptions? options = null)
        {
            options?.Freeze();
            return ActiveExpression<TArg1, TArg2, TArg3, TResult>.Create(expression, arg1, arg2, arg3, options);
        }

        /// <summary>
        /// Gets the string representation for the value of a node
        /// </summary>
        /// <param name="fault">The fault for the node</param>
        /// <param name="deferred"><c>true</c> if evaluation of the node has been deferred; otherwise, <c>false</c></param>
        /// <param name="value">The value for the node</param>
        /// <returns>The string representation of the node's value</returns>
        protected static string GetValueString(Exception? fault, bool deferred, object? value)
        {
            if (fault is { })
                return $"[{fault.GetType().Name}: {fault.Message}]";
            if (deferred)
                return "?";
            if (value is null)
                return "null";
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
            if (value is char ch)
            {
                return ch switch
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
                };
            }
            if (value is DateTime dt)
                return $"new System.DateTime({dt.Ticks}, System.DateTimeKind.{dt.Kind})";
            if (value is TimeSpan ts)
                return $"new System.TimeSpan({ts.Ticks})";
            if (value is Guid guid)
                return $"new System.Guid(\"{guid}\")";
            return $"{value}";
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
                case MethodCallExpression methodCallExpression:
                    return methodCallExpression.Object is null ? Expression.Call(methodCallExpression.Method, methodCallExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument))) : Expression.Call(ReplaceParameters(parameterTranslation, methodCallExpression.Object), methodCallExpression.Method, methodCallExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument)));
                case NewArrayExpression newArrayInitExpression when newArrayInitExpression.NodeType == ExpressionType.NewArrayInit:
                    return Expression.NewArrayInit(newArrayInitExpression.Type.GetElementType(), newArrayInitExpression.Expressions.Select(expression => ReplaceParameters(parameterTranslation, expression)));
                case NewExpression newExpression:
                    var newArguments = newExpression.Arguments.Select(argument => ReplaceParameters(parameterTranslation, argument));
                    return newExpression.Members is null ? Expression.New(newExpression.Constructor, newArguments) : Expression.New(newExpression.Constructor, newArguments, newExpression.Members);
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
        public static bool operator ==(ActiveExpression a, ActiveExpression b)
        {
            if (a is ActiveAndAlsoExpression andAlsoA && b is ActiveAndAlsoExpression andAlsoB)
                return andAlsoA == andAlsoB;
            if (a is ActiveCoalesceExpression coalesceA && b is ActiveCoalesceExpression coalesceB)
                return coalesceA == coalesceB;
            if (a is ActiveOrElseExpression orElseA && b is ActiveOrElseExpression orElseB)
                return orElseA == orElseB;
            if (a is ActiveBinaryExpression binaryA && b is ActiveBinaryExpression binaryB)
                return binaryA == binaryB;
            if (a is ActiveConditionalExpression conditionalA && b is ActiveConditionalExpression conditionalB)
                return conditionalA == conditionalB;
            if (a is ActiveConstantExpression constantA && b is ActiveConstantExpression constantB)
                return constantA == constantB;
            if (a is ActiveInvocationExpression invocationA && b is ActiveInvocationExpression invocationB)
                return invocationA == invocationB;
            if (a is ActiveIndexExpression indexA && b is ActiveIndexExpression indexB)
                return indexA == indexB;
            if (a is ActiveMemberExpression memberA && b is ActiveMemberExpression memberB)
                return memberA == memberB;
            if (a is ActiveMethodCallExpression methodCallA && b is ActiveMethodCallExpression methodCallB)
                return methodCallA == methodCallB;
            if (a is ActiveNewArrayInitExpression newArrayInitA && b is ActiveNewArrayInitExpression newArrayInitB)
                return newArrayInitA == newArrayInitB;
            if (a is ActiveNewExpression newA && b is ActiveNewExpression newB)
                return newA == newB;
            if (a is ActiveTypeBinaryExpression typeBinaryA && b is ActiveTypeBinaryExpression typeBinaryB)
                return typeBinaryA == typeBinaryB;
            if (a is ActiveUnaryExpression unaryA && b is ActiveUnaryExpression unaryB)
                return unaryA == unaryB;
            return a is null && b is null;
        }

        /// <summary>
        /// Determines whether two active expression tree nodes are different
        /// </summary>
        /// <param name="a">The first node to compare, or null</param>
        /// <param name="b">The second node to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveExpression a, ActiveExpression b) => !(a == b);

        /// <summary>
        /// Gets/sets the method that will be invoked during the active expression creation process to optimize expressions (default is null)
        /// </summary>
        public static Func<Expression, Expression>? Optimizer { get; set; }
    }

    /// <summary>
    /// Represents an active evaluation of a lambda expression
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the lambda expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TResult> : SyncDisposable, IActiveExpression<TResult>, IEquatable<ActiveExpression<TResult>>
    {
        ActiveExpression(ActiveExpression activeExpression, ActiveExpressionOptions? options, EquatableList<object?> arguments)
        {
            this.activeExpression = activeExpression;
            Options = options;
            this.arguments = arguments;
            fault = this.activeExpression.Fault;
            val = this.activeExpression.Value is TResult value ? value : default;
            this.activeExpression.PropertyChanged += ExpressionPropertyChanged;
        }

        readonly ActiveExpression activeExpression;
        readonly EquatableList<object?> arguments;
        int disposalCount;
        Exception? fault;
        TResult? val;

        /// <summary>
        /// Gets the arguments that were passed to the lambda expression
        /// </summary>
        public IReadOnlyList<object?> Arguments => arguments;

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
            activeExpression.PropertyChanged -= ExpressionPropertyChanged;
            activeExpression.Dispose();
            return true;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        /// <param name="obj">The object to compare with the current object</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
        public override bool Equals(object? obj) => obj is ActiveExpression<TResult> other && Equals(other);

        /// <summary>
        /// Determines whether the specified <see cref="ActiveExpression{TResult}"/> is equal to the current <see cref="ActiveExpression{TResult}"/>
        /// </summary>
        /// <param name="other">The other <see cref="ActiveExpression{TResult}"/></param>
        /// <returns><c>true</c> if the specified <see cref="ActiveExpression{TResult}"/> is equal to the current <see cref="ActiveExpression{TResult}"/>; otherwise, <c>false</c></returns>
        public bool Equals(ActiveExpression<TResult> other) => activeExpression == other.activeExpression;

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = activeExpression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = activeExpression.Value is TResult typedValue ? typedValue : default;
        }

        /// <summary>
        /// Gets the hash code for this active expression
        /// </summary>
        /// <returns>The hash code for this active expression</returns>
        public override int GetHashCode() => HashCode.Combine(typeof(ActiveExpression<TResult>), activeExpression);

        /// <summary>
        /// Returns a string that represents this active expression
        /// </summary>
        /// <returns>A string that represents this active expression</returns>
        public override string ToString() => activeExpression.ToString();

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<InstancesKey, ActiveExpression<TResult>> instances = new Dictionary<InstancesKey, ActiveExpression<TResult>>();

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
        public static bool operator ==(ActiveExpression<TResult> a, ActiveExpression<TResult> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two active expressions are different
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpression<TResult> a, ActiveExpression<TResult> b) => !(a == b);

        record InstancesKey(ActiveExpression ActiveExpression, EquatableList<object?> Args);
    }

    /// <summary>
    /// Represents an active evaluation of a strongly-typed lambda expression with a single argument
    /// </summary>
    /// <typeparam name="TArg">The type of the argument passed to the lambda expression</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TArg, TResult> : SyncDisposable, IActiveExpression<TArg, TResult>, IEquatable<ActiveExpression<TArg, TResult>>
    {
        ActiveExpression(ActiveExpression activeExpression, ActiveExpressionOptions? options, TArg? arg)
        {
            this.activeExpression = activeExpression;
            Options = options;
            arguments = new EquatableList<object?>(new object?[] { arg });
            Arg = arg;
            fault = this.activeExpression.Fault;
            val = this.activeExpression.Value is TResult value ? value : default;
            this.activeExpression.PropertyChanged += ExpressionPropertyChanged;
        }

        readonly ActiveExpression activeExpression;
        readonly EquatableList<object?> arguments;
        int disposalCount;
        Exception? fault;
        TResult? val;

        /// <summary>
        /// Gets the argument that was passed to the lambda expression
        /// </summary>
        public TArg? Arg { get; }

        /// <summary>
        /// Gets the arguments that were passed to the lambda expression
        /// </summary>
        public IReadOnlyList<object?> Arguments => arguments;

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
            activeExpression.PropertyChanged -= ExpressionPropertyChanged;
            activeExpression.Dispose();
            return true;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        /// <param name="obj">The object to compare with the current object</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
        public override bool Equals(object? obj) => obj is ActiveExpression<TArg, TResult> other && Equals(other);

        /// <summary>
        /// Determines whether the specified <see cref="ActiveExpression{TArg, TResult}"/> is equal to the current <see cref="ActiveExpression{TArg, TResult}"/>
        /// </summary>
        /// <param name="other">The <see cref="ActiveExpression{TArg, TResult}"/> to compare with the current <see cref="ActiveExpression{TArg, TResult}"/></param>
        /// <returns><c>true</c> if the specified <see cref="ActiveExpression{TArg, TResult}"/> is equal to the current <see cref="ActiveExpression{TArg, TResult}"/>; otherwise, <c>false</c></returns>
        public bool Equals(ActiveExpression<TArg, TResult> other) => activeExpression == other.activeExpression;

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = activeExpression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = activeExpression.Value is TResult typedValue ? typedValue : default;
        }

        /// <summary>
        /// Gets the hash code for this active expression
        /// </summary>
        /// <returns>The hash code for this active expression</returns>
        public override int GetHashCode() => HashCode.Combine(typeof(ActiveExpression<TArg, TResult>), activeExpression);

        /// <summary>
        /// Returns a string that represents this active expression
        /// </summary>
        /// <returns>A string that represents this active expression</returns>
        public override string ToString() => activeExpression.ToString();

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<InstancesKey, ActiveExpression<TArg?, TResult>> instances = new Dictionary<InstancesKey, ActiveExpression<TArg?, TResult>>();

        internal static ActiveExpression<TArg?, TResult> Create(LambdaExpression expression, TArg? arg, ActiveExpressionOptions? options = null)
        {
            var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg), options, false);
            var key = new InstancesKey(activeExpression, arg);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var instance))
                {
                    instance = new ActiveExpression<TArg?, TResult>(activeExpression, options, arg);
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
        public static bool operator ==(ActiveExpression<TArg, TResult> a, ActiveExpression<TArg, TResult> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two active expressions are different
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpression<TArg, TResult> a, ActiveExpression<TArg, TResult> b) => !(a == b);

        record InstancesKey(ActiveExpression ActiveExpression, TArg? Arg);
    }

    /// <summary>
    /// Represents an active evaluation of a strongly-typed lambda expression with two arguments
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument passed to the lambda expression</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the lambda expression</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TArg1, TArg2, TResult> : SyncDisposable, IActiveExpression<TArg1, TArg2, TResult>, IEquatable<ActiveExpression<TArg1, TArg2, TResult>>
    {
        ActiveExpression(ActiveExpression activeExpression, ActiveExpressionOptions? options, TArg1? arg1, TArg2? arg2)
        {
            this.activeExpression = activeExpression;
            Options = options;
            arguments = new EquatableList<object?>(new object?[] { arg1, arg2 });
            Arg1 = arg1;
            Arg2 = arg2;
            fault = this.activeExpression.Fault;
            val = this.activeExpression.Value is TResult value ? value : default;
            this.activeExpression.PropertyChanged += ExpressionPropertyChanged;
        }

        readonly ActiveExpression activeExpression;
        readonly EquatableList<object?> arguments;
        int disposalCount;
        Exception? fault;
        TResult? val;

        /// <summary>
        /// Gets the arguments that were passed to the lambda expression
        /// </summary>
        public IReadOnlyList<object?> Arguments => arguments;

        /// <summary>
        /// Gets the first argument that was passed to the lambda expression
        /// </summary>
        public TArg1? Arg1 { get; }

        /// <summary>
        /// Gets the second argument that was passed to the lambda expression
        /// </summary>
        public TArg2? Arg2 { get; }

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
            activeExpression.PropertyChanged -= ExpressionPropertyChanged;
            activeExpression.Dispose();
            return true;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        /// <param name="obj">The object to compare with the current object</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
        public override bool Equals(object? obj) => obj is ActiveExpression<TArg1, TArg2, TResult> other && Equals(other);

        /// <summary>
        /// Determines whether the specified <see cref="ActiveExpression{TArg1, TArg2, TResult}"/> is equal to the current object
        /// </summary>
        /// <param name="other">The object to compare with the current object</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
        public bool Equals(ActiveExpression<TArg1, TArg2, TResult> other) => activeExpression == other.activeExpression;

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = activeExpression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = activeExpression.Value is TResult typedValue ? typedValue : default;
        }

        /// <summary>
        /// Gets the hash code for this active expression
        /// </summary>
        /// <returns>The hash code for this active expression</returns>
        public override int GetHashCode() => HashCode.Combine(typeof(ActiveExpression<TArg1, TArg2, TResult>), activeExpression);

        /// <summary>
        /// Returns a string that represents this active expression
        /// </summary>
        /// <returns>A string that represents this active expression</returns>
        public override string ToString() => activeExpression.ToString();

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<InstancesKey, ActiveExpression<TArg1?, TArg2?, TResult>> instances = new Dictionary<InstancesKey, ActiveExpression<TArg1?, TArg2?, TResult>>();

        internal static ActiveExpression<TArg1?, TArg2?, TResult> Create(LambdaExpression expression, TArg1? arg1, TArg2? arg2, ActiveExpressionOptions? options = null)
        {
            var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg1, arg2), options, false);
            var key = new InstancesKey(activeExpression, arg1, arg2);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var instance))
                {
                    instance = new ActiveExpression<TArg1?, TArg2?, TResult>(activeExpression, options, arg1, arg2);
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
        public static bool operator ==(ActiveExpression<TArg1, TArg2, TResult> a, ActiveExpression<TArg1, TArg2, TResult> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two active expressions are different
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpression<TArg1, TArg2, TResult> a, ActiveExpression<TArg1, TArg2, TResult> b) => !(a == b);

        record InstancesKey(ActiveExpression ActiveExpression, TArg1? Arg1, TArg2? Arg2);
    }

    /// <summary>
    /// Represents an active evaluation of a strongly-typed lambda expression with three arguments
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument passed to the lambda expression</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the lambda expression</typeparam>
    /// <typeparam name="TArg3">The type of the third argument passed to the lambda expression</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
    public class ActiveExpression<TArg1, TArg2, TArg3, TResult> : SyncDisposable, IActiveExpression<TArg1, TArg2, TArg3, TResult>, IEquatable<ActiveExpression<TArg1, TArg2, TArg3, TResult>>
    {
        ActiveExpression(ActiveExpression activeExpression, ActiveExpressionOptions? options, TArg1? arg1, TArg2? arg2, TArg3? arg3)
        {
            this.activeExpression = activeExpression;
            Options = options;
            arguments = new EquatableList<object?>(new object?[] { arg1, arg2, arg3 });
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            fault = this.activeExpression.Fault;
            val = this.activeExpression.Value is TResult value ? value : default;
            this.activeExpression.PropertyChanged += ExpressionPropertyChanged;
        }

        readonly ActiveExpression activeExpression;
        readonly EquatableList<object?> arguments;
        int disposalCount;
        Exception? fault;
        TResult? val;

        /// <summary>
        /// Gets the arguments that were passed to the lambda expression
        /// </summary>
        public IReadOnlyList<object?> Arguments => arguments;

        /// <summary>
        /// Gets the first argument that was passed to the lambda expression
        /// </summary>
        public TArg1? Arg1 { get; }

        /// <summary>
        /// Gets the second argument that was passed to the lambda expression
        /// </summary>
        public TArg2? Arg2 { get; }

        /// <summary>
        /// Gets the third argument that was passed to the lambda expression
        /// </summary>
        public TArg3? Arg3 { get; }

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
            activeExpression.PropertyChanged -= ExpressionPropertyChanged;
            activeExpression.Dispose();
            return true;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        /// <param name="obj">The object to compare with the current object</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
        public override bool Equals(object? obj) => obj is ActiveExpression<TArg1, TArg2, TArg3, TResult> other && Equals(other);

        /// <summary>
        /// Determines whether the specified <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/> is equal to the current <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/>
        /// </summary>
        /// <param name="other">The <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/> to compare with the current <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/></param>
        /// <returns><c>true</c> if the specified <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/> is equal to the current <see cref="ActiveExpression{TArg1, TArg2, TArg3, TResult}"/>; otherwise, <c>false</c></returns>
        public bool Equals(ActiveExpression<TArg1, TArg2, TArg3, TResult> other) => activeExpression == other.activeExpression;

        void ExpressionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Fault))
                Fault = activeExpression.Fault;
            else if (e.PropertyName == nameof(Value))
                Value = activeExpression.Value is TResult typedValue ? typedValue : default;
        }

        /// <summary>
        /// Gets the hash code for this active expression
        /// </summary>
        /// <returns>The hash code for this active expression</returns>
        public override int GetHashCode() => HashCode.Combine(typeof(ActiveExpression<TArg1, TArg2, TArg3, TResult>), activeExpression);

        /// <summary>
        /// Returns a string that represents this active expression
        /// </summary>
        /// <returns>A string that represents this active expression</returns>
        public override string ToString() => activeExpression.ToString();

        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<InstancesKey, ActiveExpression<TArg1?, TArg2?, TArg3?, TResult>> instances = new Dictionary<InstancesKey, ActiveExpression<TArg1?, TArg2?, TArg3?, TResult>>();

        internal static ActiveExpression<TArg1?, TArg2?, TArg3?, TResult> Create(LambdaExpression expression, TArg1? arg1, TArg2? arg2, TArg3? arg3, ActiveExpressionOptions? options = null)
        {
            var activeExpression = ActiveExpression.Create(ActiveExpression.ReplaceParameters(expression, arg1, arg2, arg3), options, false);
            var key = new InstancesKey(activeExpression, arg1, arg2, arg3);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var instance))
                {
                    instance = new ActiveExpression<TArg1?, TArg2?, TArg3?, TResult>(activeExpression, options, arg1, arg2, arg3);
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
        public static bool operator ==(ActiveExpression<TArg1, TArg2, TArg3, TResult> a, ActiveExpression<TArg1, TArg2, TArg3, TResult> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two active expressions are different
        /// </summary>
        /// <param name="a">The first expression to compare, or null</param>
        /// <param name="b">The second expression to compare, or null</param>
        /// <returns><c>true</c> is <paramref name="a"/> is different from <paramref name="b"/>; otherwise, <c>false</c></returns>
        public static bool operator !=(ActiveExpression<TArg1, TArg2, TArg3, TResult> a, ActiveExpression<TArg1, TArg2, TArg3, TResult> b) => !(a == b);

        record InstancesKey(ActiveExpression ActiveExpression, TArg1? Arg1, TArg2? Arg2, TArg3? Arg3);
    }
}
