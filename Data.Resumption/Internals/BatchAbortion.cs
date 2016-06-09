using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Resumption
{
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