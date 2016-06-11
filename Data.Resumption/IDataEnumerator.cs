using System;

namespace Data.Resumption
{
    /// <summary>
    /// A stateful iterator for IDataEnumerables.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDataEnumerator<T> : IDisposable
    {
        /// <summary>
        /// Get a task which will yield either the next element in the sequence, or the end of the sequence.
        /// </summary>
        /// <remarks>
        /// Callers should not call MoveNext() twice without awaiting the returned data task in between.
        /// </remarks>
        /// <returns></returns>
        IDataTask<DataTaskYield<T>> MoveNext();
    }
}