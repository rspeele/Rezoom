namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents the yielding of a single value within a data enumerable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class YieldEnumerable<T> : IDataEnumerable<T>
    {
        private readonly T _value;

        public YieldEnumerable(T value)
        {
            _value = value;
        }

        private class YieldEnumerator : IDataEnumerator<T>
        {
            private readonly T _value;
            private bool _moved;

            public YieldEnumerator(T value)
            {
                _value = value;
            }

            public DataTask<DataTaskYield<T>> MoveNext()
            {
                if (_moved) return DataTask.Return(new DataTaskYield<T>());
                _moved = true;
                return DataTask.Return(new DataTaskYield<T>(_value));
            }

            public void Dispose()
            {
            }
        }

        public IDataEnumerator<T> GetEnumerator() => new YieldEnumerator(_value);
    }
}
