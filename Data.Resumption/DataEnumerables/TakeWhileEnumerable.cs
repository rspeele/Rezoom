using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Truncates a sequence to only the part before the predicate returns false.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class TakeWhileEnumerable<T> : IDataEnumerable<T>
    {
        private readonly IDataEnumerable<T> _inputs;
        private readonly Func<T, IDataTask<bool>> _predicate;

        public TakeWhileEnumerable(IDataEnumerable<T> inputs, Func<T, IDataTask<bool>> predicate)
        {
            _inputs = inputs;
            _predicate = predicate;
        }

        private class TakeWhileEnumerator : IDataEnumerator<T>
        {
            private readonly IDataEnumerator<T> _inputs;
            private readonly Func<T, IDataTask<bool>> _predicate;

            public TakeWhileEnumerator(IDataEnumerator<T> inputs, Func<T, IDataTask<bool>> predicate)
            {
                _inputs = inputs;
                _predicate = predicate;
            }

            public IDataTask<DataTaskYield<T>> MoveNext()
                => _inputs.MoveNext()
                    .Bind(yield => !yield.HasValue ? DataTask.Return(yield)
                        : _predicate(yield.Value).Bind(shouldContinue =>
                            shouldContinue
                            ? DataTask.Return(yield)
                            : DataTask.Return(new DataTaskYield<T>())));

            public void Dispose() => _inputs.Dispose();
        }

        public IDataEnumerator<T> GetEnumerator()
            => new TakeWhileEnumerator(_inputs.GetEnumerator(), _predicate);
    }
}
