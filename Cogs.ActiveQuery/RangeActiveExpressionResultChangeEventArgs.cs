using System;
using System.Diagnostics.CodeAnalysis;

namespace Cogs.ActiveQuery
{
    class RangeActiveExpressionResultChangeEventArgs<TElement, TResult> : EventArgs
    {
        public RangeActiveExpressionResultChangeEventArgs([MaybeNull] TElement element, [MaybeNull] TResult result)
        {
            Element = element;
            Result = result;
        }

        public RangeActiveExpressionResultChangeEventArgs([MaybeNull] TElement element, [MaybeNull] TResult result, int count) : this(element, result) => Count = count;

        public int Count { get; }
        [MaybeNull]
        public TElement Element { get; }
        [MaybeNull]
        public TResult Result { get; }
    }
}
