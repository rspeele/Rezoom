namespace Data.Resumption
{
    public struct DataTaskYield<T>
    {
        public DataTaskYield(T value)
        {
            Value = value;
            HasValue = true;
        }
        public bool HasValue { get; }
        /// <summary>
        /// The value yielded from this iteration.
        /// </summary>
        public T Value { get; }
    }
}