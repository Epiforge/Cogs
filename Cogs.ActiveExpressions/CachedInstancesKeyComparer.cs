using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class CachedInstancesKeyComparer<TExpression> : IEqualityComparer<CachedInstancesKey<TExpression>> where TExpression : Expression
    {
        public bool Equals(CachedInstancesKey<TExpression> x, CachedInstancesKey<TExpression> y) =>
            ExpressionEqualityComparer.Default.Equals(x.Expression, y.Expression) && ((x.Options is null && y.Options is null) || (x.Options is { } && y.Options is { } && x.Options.Equals(y.Options)));

        public int GetHashCode(CachedInstancesKey<TExpression> obj) =>
            HashCode.Combine(ExpressionEqualityComparer.Default.GetHashCode(obj.Expression), obj.Options);
    }
}
