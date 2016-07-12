using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Resumption
{
    /// <summary>
    /// Represents a pair of <see cref="Batch{T}"/>s stuck together.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class BatchBranch2<T> : Batch<T>
    {
        public BatchBranch2(Batch<T> left, Batch<T> right)
        {
            Left = left;
            Right = right;
        }

        public Batch<T> Left { get; }
        public Batch<T> Right { get; }

        public override Batch<TOut> Map<TOut>(Func<T, TOut> mapping)
            => new BatchBranch2<TOut>(Left.Map(mapping), Right.Map(mapping));

        internal override BatchBranch2<T> AssumeBranch2() => this;

        internal override BatchLeaf<T> AssumeLeaf()
        {
            throw new InvalidOperationException("Branch2 assumed to be a Leaf");
        }

        internal override BatchBranchN<T> AssumeBranchN()
        {
            throw new InvalidOperationException("Branch2 assumed to be a BranchN");
        }
    }
}