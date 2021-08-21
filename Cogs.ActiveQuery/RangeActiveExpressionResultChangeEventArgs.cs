namespace Cogs.ActiveQuery;

class RangeActiveExpressionResultChangeEventArgs<TElement, TResult> : EventArgs
{
    public RangeActiveExpressionResultChangeEventArgs(TElement element, TResult? result)
    {
        Element = element;
        Result = result;
    }

    public RangeActiveExpressionResultChangeEventArgs(TElement element, TResult? result, int count) : this(element, result) => Count = count;

    public int Count { get; }

    public TElement Element { get; }

    public TResult? Result { get; }
}
