using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Resumption
{
    /// <summary>
    /// Represents an array of <see cref="Batch{T}"/>s stuck together.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class BatchBranchN<T> : Batch<T>
    {
        public BatchBranchN(IReadOnlyList<Batch<T>> children)
        {
            Children = children;
        }

        public IReadOnlyList<Batch<T>> Children { get; }

        public override Batch<TOut> Map<TOut>(Func<T, TOut> mapping)
            => new BatchBranchN<TOut>(Children.Select(batch => batch.Map(mapping)).ToList());

        internal override BatchBranchN<T> AssumeBranchN() => this;

        internal override BatchLeaf<T> AssumeLeaf()
        {
            throw new InvalidOperationException("BranchN assumed to be a Branch2");
        }

        internal override BatchBranch2<T> AssumeBranch2()
        {
            throw new InvalidOperationException("BranchN assumed to be a Branch2");
        }

        public override IEnumerator<T> GetEnumerator() => Children.SelectMany(c => c).GetEnumerator();
    }
}