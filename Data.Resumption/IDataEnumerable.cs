using System.Collections.Generic;

namespace Data.Resumption
{
    /// <summary>
    /// Represents a sequence of <typeparamref name="T"/> values produced
    /// via asynchronous `IDataTask`s. This is not quite the same thing
    /// as an `IEnumerable&lt;IDataTask&lt;<typeparamref name="T"/>&gt;&gt;`
    /// because the act of iterating can itself be asynchronous.
    /// </summary>
    /// <remarks>
    /// In some cases this may be the only type that correctly models how values are obtained.
    /// In other cases, an <see cref="IEnumerable{T}"/> of <see cref="DataTask{TResult}"/>s would be
    /// more appropriate, since it can be executed more efficiently.
    /// 
    /// For example, suppose you were making calls to a web API
    /// in which each call required an ID as a parameter and returned:
    ///     1. Some data about the entity with the requested ID
    ///     2. The ID of the next entity in the sequence
    /// 
    /// If you only had the initial ID, an <see cref="IDataEnumerable{T}"/> would be a good fit.
    /// This is because you would need to make the asynchronous calls to iterate the sequence.
    /// You couldn't possibly make the requests in parallel since each one depends on the previous one.
    /// 
    /// However, if you already had an array of IDs to obtain, it would be more efficient to simply
    /// generate an <see cref="IEnumerable{T}"/> of <see cref="DataTask{TResult}"/>s, which you could then execute
    /// concurrently to efficiently obtain the results.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public interface IDataEnumerable<T>
    {
        /// <summary>
        /// Get a stateful enumerator for the sequence.
        /// </summary>
        /// <remarks>
        /// This may be called multiple times for the same sequence to iterate it more than once.
        /// Each enumerator has its own state, and won't modify the state of the <see cref="IDataEnumerable{T}"/>.
        /// 
        /// However, depending on the author of the sequence, it may of course modify some other shared state
        /// like globals or captured closure variables.
        /// </remarks>
        /// <returns></returns>
        IDataEnumerator<T> GetEnumerator();
    }
}
