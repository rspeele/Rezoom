using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Resumption
{
    /// <summary>
    /// Represents an invalid branch within a <see cref="Batch{T}"/>, used when aborting a <see cref="DataTask{TResult}"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DataTask{TResult}"/> will either complete or attempt to treat this branch as another, valid
    /// type of <see cref="Batch{T}"/>, and receive a <see cref="DataTaskAbortException"/> for doing so.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    internal sealed class BatchAbortion<T> : Batch<T>
    {
        public override Batch<TOut> Map<TOut>(Func<T, TOut> mapping) => new BatchAbortion<TOut>();

        internal override BatchLeaf<T> AssumeLeaf()
        {
            throw new DataTaskAbortException();
        }

        internal override BatchBranchN<T> AssumeBranchN()
        {
            throw new DataTaskAbortException();
        }

        internal override BatchBranch2<T> AssumeBranch2()
        {
            throw new DataTaskAbortException();
        }

        public override IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();
    }
}