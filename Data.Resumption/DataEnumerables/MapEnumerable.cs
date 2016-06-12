using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// A data enumerable with a synchronous predicate function mapped over its elements.
    /// </summary>
    /// <remarks>
    /// This could be implemented in terms of BindEnumerable but is slightly more efficient this way.
    /// </remarks>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    internal class MapEnumerable<TIn, TOut> : IDataEnumerable<TOut>
    {
        private readonly IDataEnumerable<TIn> _inputs;
        private readonly Func<TIn, TOut> _mapping;

        public MapEnumerable(IDataEnumerable<TIn> inputs, Func<TIn, TOut> mapping)
        {
            _inputs = inputs;
            _mapping = mapping;
        }

        private class MapEnumerator : IDataEnumerator<TOut>
        {
            private readonly IDataEnumerator<TIn> _inputs;
            private readonly Func<TIn, TOut> _mapping;

            public MapEnumerator(IDataEnumerator<TIn> inputs, Func<TIn, TOut> mapping)
            {
                _inputs = inputs;
                _mapping = mapping;
            }

            public IDataTask<DataTaskYield<TOut>> MoveNext()
                => _inputs.MoveNext().Select(y =>
                    y.HasValue
                        ? new DataTaskYield<TOut>(_mapping(y.Value))
                        : new DataTaskYield<TOut>());

            public void Dispose() => _inputs.Dispose();
        }

        public IDataEnumerator<TOut> GetEnumerator()
            => new MapEnumerator(_inputs.GetEnumerator(), _mapping);
    }
}
