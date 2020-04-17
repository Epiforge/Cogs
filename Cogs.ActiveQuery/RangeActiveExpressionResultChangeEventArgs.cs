using System;
using System.Diagnostics.CodeAnalysis;

namespace Cogs.ActiveQuery
{
    class RangeActiveExpressionResultChangeEventArgs<TElement, TResult> : EventArgs
    {
        public RangeActiveExpressionResultChangeEventArgs([AllowNull] TElement element, [AllowNull] TResult result)
        {
            Element = element;
            Result = result;
        }

        public RangeActiveExpressionResultChangeEventArgs([AllowNull] TElement element, [AllowNull] TResult result, int count) : this(element, result) => Count = count;

        public int Count { get; }

        [AllowNull, MaybeNull]
        public TElement Element { get; }

        [AllowNull, MaybeNull]
        public TResult Result { get; }
    }
}
