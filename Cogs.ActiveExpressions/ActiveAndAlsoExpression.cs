using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class ActiveAndAlsoExpression : ActiveBinaryExpression, IEquatable<ActiveAndAlsoExpression>
    {
        public ActiveAndAlsoExpression(BinaryExpression binaryExpression, ActiveExpressionOptions? options, CachedInstancesKey<BinaryExpression> instancesKey, bool deferEvaluation) : base(binaryExpression, options, instancesKey, deferEvaluation, false)
        {
        }

        public override bool Equals(object? obj) => obj is ActiveAndAlsoExpression other && Equals(other);

        public bool Equals(ActiveAndAlsoExpression other) => left == other.left && right == other.right && Equals(options, other.options);

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            if (leftFault is { })
                Fault = leftFault;
            else if (!(left.Value is bool leftBool && leftBool))
                Value = false;
            else
            {
                var rightFault = right.Fault;
                if (rightFault is { })
                    Fault = rightFault;
                else
                    Value = right.Value is bool rightBool && rightBool;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveAndAlsoExpression), left, right, options);

        public override string ToString() => $"({left} && {right}) {ToStringSuffix}";

        public static bool operator ==(ActiveAndAlsoExpression a, ActiveAndAlsoExpression b) => a.Equals(b);

        [ExcludeFromCodeCoverage]
        public static bool operator !=(ActiveAndAlsoExpression a, ActiveAndAlsoExpression b) => !(a == b);
    }
}
