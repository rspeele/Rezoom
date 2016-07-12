using System;
using System.Collections.Generic;

namespace Data.Resumption
{
    /// <summary>
    /// An immutable tree of Ts.
    /// Used to represent a batch of data requests.
    /// </summary>
    /// <remarks>
    /// A tree structure is used because it allows a pair of batches to be combined into one in O(1) time
    /// without undue copying. It also can be easily mapped over to get a matching tree shape to correlate responses.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public abstract class Batch<T>
    {
        /// <summary>
        /// Map a function over this batch to get another batch with the mapped elements
        /// arranged in the same tree structure.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public abstract Batch<TOut> Map<TOut>(Func<T, TOut> mapping);
        /// <summary>
        /// Unsafely cast to a <see cref="BatchLeaf{T}"/>, throwing an exception if it is another type.
        /// </summary>
        /// <returns></returns>
        internal abstract BatchLeaf<T> AssumeLeaf();
        /// <summary>
        /// Unsafely cast to a <see cref="BatchBranchN{T}"/>, throwing an exception if it is another type.
        /// </summary>
        /// <returns></returns>
        internal abstract BatchBranchN<T> AssumeBranchN();
        /// <summary>
        /// Unsafely cast to a <see cref="BatchBranch2{T}"/>, throwing an exception if it is another type.
        /// </summary>
        /// <returns></returns>
        internal abstract BatchBranch2<T> AssumeBranch2();
    }
}