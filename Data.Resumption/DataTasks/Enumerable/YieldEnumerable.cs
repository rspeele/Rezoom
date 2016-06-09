namespace Data.Resumption.DataTasks.Enumerable
{
    internal class YieldEnumerable<T> : IDataEnumerable<T>
    {
        private readonly T _value;

        public YieldEnumerable(T value)
        {
            _value = value;
        }

        public IDataTask<DataTaskYield<T>?> Yield()
            => DataTask.Return<DataTaskYield<T>?>
                (new DataTaskYield<T>(_value, new ZeroEnumerable<T>()));
    }
}
