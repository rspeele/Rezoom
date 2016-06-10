using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents monadic binding of data enumerables (like SelectMany).
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    internal class BindEnumerable<TIn, TOut> : IDataEnumerable<TOut>
    {
        private readonly IDataEnumerable<TIn> _inputs;
        private readonly Func<TIn, IDataEnumerable<TOut>> _selector;

        public BindEnumerable(IDataEnumerable<TIn> inputs, Func<TIn, IDataEnumerable<TOut>> selector)
        {
            _inputs = inputs;
            _selector = selector;
        }

        public IDataTask<DataTaskYield<TOut>?> Yield()
            => _inputs.Yield().SelectMany(yieldInput =>
            {
                if (!yieldInput.HasValue)
                {
                    return DataTask.Return<DataTaskYield<TOut>?>
                        (new DataTaskYield<TOut>());
                }
                var subSequence = _selector(yieldInput.Value.Value);
                var fullSequence = subSequence.Combine(() =>
                    new BindEnumerable<TIn, TOut>(yieldInput.Value.Remaining, _selector));
                return fullSequence.Yield();
            });
    }
}
