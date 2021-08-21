namespace Cogs.ActiveExpressions;

record CachedInstancesKey<TExpression>(TExpression Expression, ActiveExpressionOptions? Options) where TExpression : Expression;
