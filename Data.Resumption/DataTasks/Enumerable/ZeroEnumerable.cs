namespace Data.Resumption.DataTasks.Enumerable
{
    internal class ZeroEnumerable<T> : IDataEnumerable<T>
    {
        public IDataTask<DataTaskYield<T>?> Yield()
            => DataTask.Return<DataTaskYield<T>?>(null);
    }
}