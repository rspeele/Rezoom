namespace Data.Resumption
{
    /// <summary>
    /// Represents a value yielded from an IDataTaskEnumerable,
    /// along with an IDataTaskEnumerable which will yield the remaining values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct DataTaskYield<T>
    {
        public DataTaskYield(T value, IDataTaskEnumerable<T> remaining)
        {
            Value = value;
            Remaining = remaining;
        }
        /// <summary>
        /// The value yielded from this iteration.
        /// </summary>
        public T Value { get; }
        /// <summary>
        /// An IDataTaskEnumerable to yield the remaining values of the sequence.
        /// </summary>
        public IDataTaskEnumerable<T> Remaining { get; }
    }
}