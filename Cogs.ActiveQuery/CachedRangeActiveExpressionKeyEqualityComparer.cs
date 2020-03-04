using Cogs.ActiveExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Cogs.ActiveQuery
{
    class CachedRangeActiveExpressionKeyEqualityComparer<TResult> : IEqualityComparer<(IEnumerable source, Expression<Func<object?, TResult>> expression, ActiveExpressionOptions? options)>
    {
        public static CachedRangeActiveExpressionKeyEqualityComparer<TResult> Default { get; } = new CachedRangeActiveExpressionKeyEqualityComparer<TResult>();

        public bool Equals((IEnumerable source, Expression<Func<object?, TResult>> expression, ActiveExpressionOptions? options) x, (IEnumerable source, Expression<Func<object?, TResult>> expression, ActiveExpressionOptions? options) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.expression, y.expression) && Equals(x.options, y.options);

        public int GetHashCode((IEnumerable source, Expression<Func<object?, TResult>> expression, ActiveExpressionOptions? options) obj) =>
            HashCode.Combine(typeof((IEnumerable source, Expression<Func<object?, TResult>> expression, ActiveExpressionOptions? options)), obj.source?.GetHashCode() ?? 0, ExpressionEqualityComparer.Default.GetHashCode(obj.expression), obj.options?.GetHashCode() ?? 0);
    }

    class CachedRangeActiveExpressionKeyEqualityComparer<TElement, TResult> : IEqualityComparer<(IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions? options)>
    {
        public static CachedRangeActiveExpressionKeyEqualityComparer<TElement, TResult> Default { get; } = new CachedRangeActiveExpressionKeyEqualityComparer<TElement, TResult>();

        public bool Equals((IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions? options) x, (IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions? options) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.expression, y.expression) && Equals(x.options, y.options);

        public int GetHashCode((IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions? options) obj) =>
            HashCode.Combine(typeof((IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions? options)), obj.source?.GetHashCode() ?? 0, ExpressionEqualityComparer.Default.GetHashCode(obj.expression), obj.options?.GetHashCode() ?? 0);
    }

    class CachedRangeActiveExpressionKeyEqualityComparer<TKey, TValue, TResult> : IEqualityComparer<(IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions? options)>
    {
        public static CachedRangeActiveExpressionKeyEqualityComparer<TKey, TValue, TResult> Default { get; } = new CachedRangeActiveExpressionKeyEqualityComparer<TKey, TValue, TResult>();

        public bool Equals((IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions? options) x, (IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions? options) y) =>
            ReferenceEquals(x.source, y.source) && ExpressionEqualityComparer.Default.Equals(x.expression, y.expression) && Equals(x.options, y.options);

        public int GetHashCode((IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions? options) obj) =>
            HashCode.Combine(typeof((IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions? options)), obj.source?.GetHashCode() ?? 0, ExpressionEqualityComparer.Default.GetHashCode(obj.expression), obj.options?.GetHashCode() ?? 0);
    }
}
