namespace Cogs.ActiveExpressions;

class ActiveAndAlsoExpression : ActiveBinaryExpression, IEquatable<ActiveAndAlsoExpression>
{
    public ActiveAndAlsoExpression(CachedInstancesKey<BinaryExpression> instancesKey, ActiveExpressionOptions? options, bool deferEvaluation) : base(instancesKey, options, deferEvaluation, false)
    {
    }

    public override bool Equals(object? obj) => obj is ActiveAndAlsoExpression other && Equals(other);

    public bool Equals(ActiveAndAlsoExpression other) => left == other.left && right == other.right && Equals(options, other.options);

    protected override void Evaluate()
    {
        var leftFault = left?.Fault;
        if (leftFault is not null)
            Fault = leftFault;
        else if (!(left?.Value is bool leftBool && leftBool))
            Value = false;
        else
        {
            var rightFault = right?.Fault;
            if (rightFault is not null)
                Fault = rightFault;
            else
                Value = right?.Value is bool rightBool && rightBool;
        }
    }

    public override int GetHashCode() => HashCode.Combine(typeof(ActiveAndAlsoExpression), left, right, options);

    public override string ToString() => $"({left} && {right}) {ToStringSuffix}";

    public static bool operator ==(ActiveAndAlsoExpression a, ActiveAndAlsoExpression b) => a.Equals(b);

    [ExcludeFromCodeCoverage]
    public static bool operator !=(ActiveAndAlsoExpression a, ActiveAndAlsoExpression b) => !(a == b);
}
