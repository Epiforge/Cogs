using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class CachedInstancesKeyComparer<TExpression> : IEqualityComparer<(TExpression expression, ActiveExpressionOptions? options)> where TExpression : Expression
    {
        public bool Equals((TExpression expression, ActiveExpressionOptions? options) x, (TExpression expression, ActiveExpressionOptions? options) y) =>
            ExpressionEqualityComparer.Default.Equals(x.expression, y.expression) && ((x.options is null && y.options is null) || (x.options is { } && y.options is { } && x.options.Equals(y.options)));

        public int GetHashCode((TExpression expression, ActiveExpressionOptions? options) obj) =>
            HashCode.Combine(ExpressionEqualityComparer.Default.GetHashCode(obj.expression), obj.options);
    }
}
