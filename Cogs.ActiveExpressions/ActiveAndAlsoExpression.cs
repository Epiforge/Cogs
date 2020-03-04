using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class ActiveAndAlsoExpression : ActiveBinaryExpression, IEquatable<ActiveAndAlsoExpression>
    {
        public ActiveAndAlsoExpression(ActiveExpression left, ActiveExpression right, ActiveExpressionOptions? options, bool deferEvaluation) : base(typeof(bool), ExpressionType.AndAlso, left, right, false, null, options, deferEvaluation, false)
        {
        }

        public override bool Equals(object obj) => obj is ActiveAndAlsoExpression other && Equals(other);

        public bool Equals(ActiveAndAlsoExpression other) => left.Equals(other.left) && right.Equals(other.right) && Equals(options, other.options);

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            if (leftFault is { })
                Fault = leftFault;
            else if (!(left.Value is bool leftBool ? leftBool : false))
                Value = false;
            else
            {
                var rightFault = right.Fault;
                if (rightFault is { })
                    Fault = rightFault;
                else
                    Value = right.Value is bool rightBool ? rightBool : false;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveAndAlsoExpression), left, right);

        public override string ToString() => $"({left} && {right}) {ToStringSuffix}";

        public static bool operator ==(ActiveAndAlsoExpression? a, ActiveAndAlsoExpression? b) => EqualityComparer<ActiveAndAlsoExpression?>.Default.Equals(a, b);

        public static bool operator !=(ActiveAndAlsoExpression? a, ActiveAndAlsoExpression? b) => !EqualityComparer<ActiveAndAlsoExpression?>.Default.Equals(a, b);
    }
}
