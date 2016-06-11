namespace Data.Resumption
{
    /// <summary>
    /// Represents a sequence of <typeparamref name="T"/> values produced
    /// via asynchronous `IDataTask`s. This is not quite the same thing
    /// as an `IEnumerable&lt;IDataTask&lt;<typeparamref name="T"/>&gt;&gt;`
    /// because the act of iterating can itself be asynchronous.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDataEnumerable<T>
    {
        IDataEnumerator<T> GetEnumerator();
    }
}
