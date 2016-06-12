namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Truncation of an input enumerable to a maximum length.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class TakeEnumerable<T> : IDataEnumerable<T>
    {
        private readonly IDataEnumerable<T> _inputs;
        private readonly int _count;

        public TakeEnumerable(IDataEnumerable<T> inputs, int count)
        {
            _inputs = inputs;
            _count = count;
        }

        private class TakeEnumerator : IDataEnumerator<T>
        {
            private readonly IDataEnumerator<T> _inputs;
            private int _counter;

            public TakeEnumerator(IDataEnumerator<T> inputs, int counter)
            {
                _inputs = inputs;
                _counter = counter;
            }

            public IDataTask<DataTaskYield<T>> MoveNext()
            {
                if (_counter <= 0) return DataTask.Return(new DataTaskYield<T>());
                _counter--;
                return _inputs.MoveNext();
            }

            public void Dispose() => _inputs.Dispose();
        }

        public IDataEnumerator<T> GetEnumerator()
            => new TakeEnumerator(_inputs.GetEnumerator(), _count);
    }
}
