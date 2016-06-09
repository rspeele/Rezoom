namespace Data.Resumption
{
    /// <summary>
    /// Represents a sequence of <typeparamref name="T"/> values produced
    /// via asynchronous `IDataTask`s. This is not quite the same thing
    /// as an `IEnumerable&lt;IDataTask&lt;<typeparamref name="T"/>&gt;&gt;`
    /// because the act of iterating can itself be asynchronous.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDataTaskEnumerable<T>
    {
        /// <summary>
        /// Get an `IDataTask` which produces either:
        ///   a. The next value in this sequence, and the remaining sequence
        /// or
        ///   b. null to indicate the end of the sequence has been reached.
        /// </summary>
        /// <returns></returns>
        IDataTask<DataTaskYield<T>?> Yield();
    }
}
