using Cogs.ActiveExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Cogs.ActiveQuery
{
    static class RangeActiveExpression
    {
        public static ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult> Create<TKey, TValue, TResult>(IReadOnlyDictionary<TKey, TValue> source, Expression<Func<TKey, TValue, TResult>> expression, ActiveExpressionOptions? options = null) => ReadOnlyDictionaryRangeActiveExpression<TKey, TValue, TResult>.Create(source, expression, options);

        public static EnumerableRangeActiveExpression<TResult> Create<TResult>(IEnumerable source, Expression<Func<object?, TResult>> expression, ActiveExpressionOptions? options = null) => EnumerableRangeActiveExpression<TResult>.Create(source, expression, options);

        public static EnumerableRangeActiveExpression<TElement, TResult> Create<TElement, TResult>(IEnumerable<TElement> source, Expression<Func<TElement, TResult>> expression, ActiveExpressionOptions? options = null) => EnumerableRangeActiveExpression<TElement, TResult>.Create(source, expression, options);
    }
}
