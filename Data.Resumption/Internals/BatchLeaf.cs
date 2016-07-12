using System;

namespace Data.Resumption
{
    /// <summary>
    /// Represents a single element within a <see cref="Batch{T}"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class BatchLeaf<T> : Batch<T>
    {
        public BatchLeaf(T element)
        {
            Element = element;
        }

        public T Element { get; }

        public override Batch<TOut> Map<TOut>(Func<T, TOut> mapping) => new BatchLeaf<TOut>(mapping(Element));

        internal override BatchLeaf<T> AssumeLeaf() => this;

        internal override BatchBranchN<T> AssumeBranchN()
        {
            throw new InvalidOperationException("Leaf assumed to be a BranchN");
        }

        internal override BatchBranch2<T> AssumeBranch2()
        {
            throw new InvalidOperationException("Leaf assumed to be a Branch2");
        }
    }
}