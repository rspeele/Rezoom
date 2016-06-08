using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Resumption
{
    /// <summary>
    /// An immutable tree of Ts.
    /// Used to represent a batch of data requests, and their eventual responses in the same tree shape.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Batch<T>
    {
        public abstract Batch<TOut> Map<TOut>(Func<T, TOut> mapping);
        public abstract BatchLeaf<T> AssumeLeaf();
        public abstract BatchBranchN<T> AssumeBranchN();
        public abstract BatchBranch2<T> AssumeBranch2();

    }
    public sealed class BatchBranchN<T> : Batch<T>
    {
        public BatchBranchN(IReadOnlyList<Batch<T>> children)
        {
            Children = children;
        }

        public IReadOnlyList<Batch<T>> Children { get; }

        public override Batch<TOut> Map<TOut>(Func<T, TOut> mapping)
            => new BatchBranchN<TOut>(Children.Select(batch => batch.Map(mapping)).ToList());

        public override BatchBranchN<T> AssumeBranchN() => this;

        public override BatchLeaf<T> AssumeLeaf()
        {
            throw new InvalidOperationException("BranchN assumed to be a Branch2");
        }

        public override BatchBranch2<T> AssumeBranch2()
        {
            throw new InvalidOperationException("BranchN assumed to be a Branch2");
        }
    }
    public sealed class BatchBranch2<T> : Batch<T>
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

        public override BatchBranch2<T> AssumeBranch2() => this;

        public override BatchLeaf<T> AssumeLeaf()
        {
            throw new InvalidOperationException("Branch2 assumed to be a Leaf");
        }

        public override BatchBranchN<T> AssumeBranchN()
        {
            throw new InvalidOperationException("Branch2 assumed to be a BranchN");
        }
    }
    public sealed class BatchLeaf<T> : Batch<T>
    {
        public BatchLeaf(T element)
        {
            Element = element;
        }

        public T Element { get; }

        public override Batch<TOut> Map<TOut>(Func<T, TOut> mapping) => new BatchLeaf<TOut>(mapping(Element));

        public override BatchLeaf<T> AssumeLeaf() => this;

        public override BatchBranchN<T> AssumeBranchN()
        {
            throw new InvalidOperationException("Leaf assumed to be a BranchN");
        }

        public override BatchBranch2<T> AssumeBranch2()
        {
            throw new InvalidOperationException("Leaf assumed to be a Branch2");
        }
    }
}