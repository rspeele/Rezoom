namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents a data enumerable that does no work and yields no values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ZeroEnumerable<T> : IDataEnumerable<T>
    {
        public IDataTask<DataTaskYield<T>?> Yield()
            => DataTask.Return<DataTaskYield<T>?>(null);
    }
}