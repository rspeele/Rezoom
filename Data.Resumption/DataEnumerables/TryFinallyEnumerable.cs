using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents a data enumerable wrapped with a `finally` block.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class TryFinallyEnumerable<T> : IDataEnumerable<T>
    {
        private readonly IDataEnumerable<T> _wrapped;
        private readonly Action _onExit;

        public TryFinallyEnumerable(IDataEnumerable<T> wrapped, Action onExit)
        {
            _wrapped = wrapped;
            _onExit = onExit;
        }

        private class TryFinallyEnumerator : IDataEnumerator<T>
        {
            private readonly IDataEnumerator<T> _wrapped;
            private readonly Action _onExit;

            public TryFinallyEnumerator(IDataEnumerator<T> wrapped, Action onExit)
            {
                _wrapped = wrapped;
                _onExit = onExit;
            }

            public IDataTask<DataTaskYield<T>> MoveNext() => _wrapped.MoveNext();
            public void Dispose()
            {
                try
                {
                    _wrapped.Dispose();
                }
                finally 
                {
                    _onExit();
                }
            }
        }

        public IDataEnumerator<T> GetEnumerator()
            => new TryFinallyEnumerator(_wrapped.GetEnumerator(), _onExit);
    }
}
