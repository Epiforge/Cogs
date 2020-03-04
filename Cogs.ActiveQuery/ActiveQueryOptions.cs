using System;
using System.Linq.Expressions;

namespace Cogs.ActiveQuery
{
    /// <summary>
    /// Represents certain options governing the behavior of active query extensions
    /// </summary>
    public static class ActiveQueryOptions
    {
        internal static Expression<Func<TSource, TResult>> Optimize<TSource, TResult>(Expression<Func<TSource, TResult>> expr) => (Expression<Func<TSource, TResult>>)(Optimizer?.Invoke(expr) ?? expr);

        internal static void Optimize<TSource, TResult>(ref Expression<Func<TSource, TResult>> expr) => expr = (Expression<Func<TSource, TResult>>)(Optimizer?.Invoke(expr) ?? expr);

        internal static void Optimize<TKey, TValue, TResult>(ref Expression<Func<TKey, TValue, TResult>> expr) => expr = (Expression<Func<TKey, TValue, TResult>>)(Optimizer?.Invoke(expr) ?? expr);

        /// <summary>
        /// Gets/sets the method that will be invoked to optimize expressions passed to Active Query extension methods (default is null)
        /// </summary>
        public static Func<Expression, Expression>? Optimizer { get; set; }
    }
}
