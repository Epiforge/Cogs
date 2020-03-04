using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Cogs.ActiveExpressions
{
    class ActiveOrElseExpression : ActiveBinaryExpression, IEquatable<ActiveOrElseExpression>
    {
        public ActiveOrElseExpression(ActiveExpression left, ActiveExpression right, ActiveExpressionOptions? options, bool deferEvaluation) : base(typeof(bool), ExpressionType.OrElse, left, right, false, null, options, deferEvaluation, false)
        {
        }

        public override bool Equals(object obj) => obj is ActiveOrElseExpression other && Equals(other);

        public bool Equals(ActiveOrElseExpression other) => left.Equals(other.left) && right.Equals(other.right) && Equals(options, other.options);

        protected override void Evaluate()
        {
            var leftFault = left.Fault;
            if (leftFault is { })
                Fault = leftFault;
            else if (left.Value is bool leftBool ? leftBool : false)
                Value = true;
            else
            {
                var rightFault = right.Fault;
                if (rightFault is { })
                    Fault = rightFault;
                else
                    Value = right.Value is bool rightBool ? rightBool : false;
            }
        }

        public override int GetHashCode() => HashCode.Combine(typeof(ActiveOrElseExpression), left, right);

        public override string ToString() => $"({left} || {right}) {ToStringSuffix}";

        public static bool operator ==(ActiveOrElseExpression? a, ActiveOrElseExpression? b) => EqualityComparer<ActiveOrElseExpression?>.Default.Equals(a, b);

        public static bool operator !=(ActiveOrElseExpression? a, ActiveOrElseExpression? b) => !EqualityComparer<ActiveOrElseExpression?>.Default.Equals(a, b);
    }
}
