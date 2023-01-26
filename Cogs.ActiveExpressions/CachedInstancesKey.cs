namespace Cogs.ActiveExpressions;

sealed record CachedInstancesKey<TExpression>(TExpression Expression, ActiveExpressionOptions? Options) where TExpression : Expression;
