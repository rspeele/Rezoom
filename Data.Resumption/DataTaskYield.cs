namespace Data.Resumption
{
    /// <summary>
    /// Represents a value yielded from an IDataEnumerable,
    /// along with an IDataEnumerable which will yield the remaining values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct DataTaskYield<T>
    {
        public DataTaskYield(T value, IDataEnumerable<T> remaining)
        {
            Value = value;
            Remaining = remaining;
        }
        /// <summary>
        /// The value yielded from this iteration.
        /// </summary>
        public T Value { get; }
        /// <summary>
        /// The remaining values of the sequence.
        /// </summary>
        public IDataEnumerable<T> Remaining { get; }
    }
}