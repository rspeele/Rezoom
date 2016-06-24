using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// A data enumerable filtered by a synchronous predicate function.
    /// </summary>
    /// <remarks>
    /// This could be implemented in terms of BindEnumerable but is slightly more efficient this way.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    internal class FilterEnumerable<T> : IDataEnumerable<T>
    {
        private readonly IDataEnumerable<T> _inputs;
        private readonly Func<T, bool> _predicate;

        public FilterEnumerable(IDataEnumerable<T> inputs, Func<T, bool> predicate)
        {
            _inputs = inputs;
            _predicate = predicate;
        }

        private class FilterEnumerator : IDataEnumerator<T>
        {
            private readonly IDataEnumerator<T> _inputs;
            private readonly Func<T, bool> _predicate;

            public FilterEnumerator(IDataEnumerator<T> inputs, Func<T, bool> predicate)
            {
                _inputs = inputs;
                _predicate = predicate;
            }

            public IDataTask<DataTaskYield<T>> MoveNext()
                => _inputs.MoveNext()
                    .Bind(yield =>
                        yield.HasValue && !_predicate(yield.Value)
                        ? MoveNext()
                        : DataTask.Return(yield));

            public void Dispose() => _inputs.Dispose();
        }

        public IDataEnumerator<T> GetEnumerator()
            => new FilterEnumerator(_inputs.GetEnumerator(), _predicate);
    }
}