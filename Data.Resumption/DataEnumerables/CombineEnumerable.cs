using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents the concatenation of two data enumerables.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class CombineEnumerable<T> : IDataEnumerable<T>
    {
        private readonly IDataEnumerable<T> _first;
        private readonly Func<IDataEnumerable<T>> _second;

        public CombineEnumerable(IDataEnumerable<T> first, Func<IDataEnumerable<T>> second)
        {
            _first = first;
            _second = second;
        }

        private class CombineEnumerator : IDataEnumerator<T>
        {
            private IDataEnumerator<T> _current;
            private Func<IDataEnumerator<T>> _next;

            public CombineEnumerator(IDataEnumerator<T> current, Func<IDataEnumerator<T>> next)
            {
                _current = current;
                _next = next;
            }

            private DataTask<DataTaskYield<T>> AdvanceEnumerators()
            {
                if (_next == null) return DataTask.Return(new DataTaskYield<T>());
                _current.Dispose();
                _current = _next();
                _next = null;
                return _current.MoveNext();
            }

            public DataTask<DataTaskYield<T>> MoveNext()
                => _current.MoveNext()
                    .Bind(y => y.HasValue ? DataTask.Return(y) : AdvanceEnumerators());

            public void Dispose() => _current?.Dispose();
        }

        public IDataEnumerator<T> GetEnumerator()
            => new CombineEnumerator(_first.GetEnumerator(), () => _second().GetEnumerator());
    }
}
