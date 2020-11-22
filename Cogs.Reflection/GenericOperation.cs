using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Cogs.Reflection
{
    /// <summary>
    /// Provides methods for adding, dividing, multiplying, and/or subtracting objects
    /// </summary>
    public static class GenericOperations
    {
        internal static ConcurrentDictionary<(BinaryOperation operation, Type type), Delegate> CompiledBinaryOperationMethods = new ConcurrentDictionary<(BinaryOperation operation, Type type), Delegate>();

        /// <summary>
        /// Adds the values of two specified <typeparamref name="T"/> objects
        /// </summary>
        /// <typeparam name="T">The type of the objects for which addition is being performed</typeparam>
        /// <param name="a">The first value to add</param>
        /// <param name="b">The second value to add</param>
        /// <returns>The sum of <paramref name="a"/> and <paramref name="b"/></returns>
        public static T? Add<T>(T? a, T? b) => a is { } && b is { } ? ((Func<T?, T?, T?>)CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Add, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b) : default;

        /// <summary>
        /// Divides a specified <typeparamref name="T"/> value by another specified <typeparamref name="T"/> value
        /// </summary>
        /// <typeparam name="T">The type of the objects for which division is being performed</typeparam>
        /// <param name="a">The value to be divided</param>
        /// <param name="b">The value to divide by</param>
        /// <returns>The result of the division</returns>
        public static T? Divide<T>(T? a, T? b) => a is { } && b is { } ? ((Func<T?, T?, T?>)CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Divide, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b) : default;

        /// <summary>
        /// Multiplies two specified <typeparamref name="T"/> values
        /// </summary>
        /// <typeparam name="T">The type of the objects for which multiplication is being performed</typeparam>
        /// <param name="a">The first value to multiply</param>
        /// <param name="b">The second value to multiply</param>
        /// <returns>The product of <paramref name="a"/> and <paramref name="b"/></returns>
        public static T? Multiply<T>(T? a, T? b) => a is { } && b is { } ? ((Func<T?, T?, T?>)CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Multiply, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b) : default;

        /// <summary>
        /// Subtracts a <typeparamref name="T"/> value from another <typeparamref name="T"/> value
        /// </summary>
        /// <typeparam name="T">The type of the objects for which subtraction is being performed</typeparam>
        /// <param name="a">The value to subtract from (the minuend)</param>
        /// <param name="b">he value to subtract (the subtrahend)</param>
        /// <returns>The result of subtracting <paramref name="b"/> from <paramref name="a"/></returns>
        public static T? Subtract<T>(T? a, T? b) => a is { } && b is { } ? ((Func<T?, T?, T?>)CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Subtract, typeof(T)), CompiledBinaryOperationMethodsValueFactory))(a, b) : default;

        internal static Delegate CompiledBinaryOperationMethodsValueFactory((BinaryOperation operation, Type type) key)
        {
            var (operation, type) = key;
            var leftHand = Expression.Parameter(type);
            var rightHand = Expression.Parameter(type);
            try
            {
                var math = operation switch
                {
                    BinaryOperation.Add => Expression.Add(leftHand, rightHand),
                    BinaryOperation.Divide => Expression.Divide(leftHand, rightHand),
                    BinaryOperation.Multiply => Expression.Multiply(leftHand, rightHand),
                    BinaryOperation.Subtract => Expression.Subtract(leftHand, rightHand),
                    _ => throw new NotSupportedException(),
                };
                return Expression.Lambda(math, leftHand, rightHand).Compile();
            }
            catch (Exception ex)
            {
                return Expression.Lambda(Expression.Block(Expression.Throw(Expression.Constant(ex)), Expression.Default(type)), leftHand, rightHand).Compile();
            }
        }
    }

    /// <summary>
    /// Provides methods for adding, dividing, multiplying, and/or subtracting instances of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of objects for which operations are provided</typeparam>
    public class GenericOperations<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericOperations{T}"/> class
        /// </summary>
        public GenericOperations()
        {
            var type = typeof(T);
            add = (Func<T?, T?, T?>)GenericOperations.CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Add, type), GenericOperations.CompiledBinaryOperationMethodsValueFactory);
            divide = (Func<T?, T?, T?>)GenericOperations.CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Divide, type), GenericOperations.CompiledBinaryOperationMethodsValueFactory);
            multiply = (Func<T?, T?, T?>)GenericOperations.CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Multiply, type), GenericOperations.CompiledBinaryOperationMethodsValueFactory);
            subtract = (Func<T?, T?, T?>)GenericOperations.CompiledBinaryOperationMethods.GetOrAdd((BinaryOperation.Subtract, type), GenericOperations.CompiledBinaryOperationMethodsValueFactory);
        }

        readonly Func<T?, T?, T?> add;
        readonly Func<T?, T?, T?> divide;
        readonly Func<T?, T?, T?> multiply;
        readonly Func<T?, T?, T?> subtract;

        /// <summary>
        /// Adds the values of two specified <typeparamref name="T"/> objects
        /// </summary>
        /// <param name="a">The first value to add</param>
        /// <param name="b">The second value to add</param>
        /// <returns>The sum of <paramref name="a"/> and <paramref name="b"/></returns>
        public T? Add(T? a, T? b) => a is { } && b is { } ? add(a, b) : default;

        /// <summary>
        /// Divides a specified <typeparamref name="T"/> value by another specified <typeparamref name="T"/> value
        /// </summary>
        /// <param name="a">The value to be divided</param>
        /// <param name="b">The value to divide by</param>
        /// <returns>The result of the division</returns>
        public T? Divide(T? a, T? b) => a is { } && b is { } ? divide(a, b) : default;

        /// <summary>
        /// Multiplies two specified <typeparamref name="T"/> values
        /// </summary>
        /// <param name="a">The first value to multiply</param>
        /// <param name="b">The second value to multiply</param>
        /// <returns>The product of <paramref name="a"/> and <paramref name="b"/></returns>
        public T? Multiply(T? a, T? b) => a is { } && b is { } ? multiply(a, b) : default;

        /// <summary>
        /// Subtracts a <typeparamref name="T"/> value from another <typeparamref name="T"/> value
        /// </summary>
        /// <param name="a">The value to subtract from (the minuend)</param>
        /// <param name="b">he value to subtract (the subtrahend)</param>
        /// <returns>The result of subtracting <paramref name="b"/> from <paramref name="a"/></returns>
        public T? Subtract(T? a, T? b) => a is { } && b is { } ? subtract(a, b) : default;
    }
}
