namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents a data enumerable that does no work and yields no values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ZeroEnumerable<T> : IDataEnumerable<T>
    {
        private class ZeroEnumerator : IDataEnumerator<T>
        {
            public DataTask<DataTaskYield<T>> MoveNext() => DataTask.Return(new DataTaskYield<T>());

            public void Dispose()
            {
            }
        }
        public IDataEnumerator<T> GetEnumerator() => new ZeroEnumerator();
    }
}