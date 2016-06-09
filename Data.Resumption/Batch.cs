using System;
using System.Collections;
using System.Collections.Generic;

namespace Data.Resumption
{
    /// <summary>
    /// An immutable tree of Ts.
    /// Used to represent a batch of data requests, and their eventual responses in the same tree shape.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Batch<T> : IEnumerable<T>
    {
        public abstract Batch<TOut> Map<TOut>(Func<T, TOut> mapping);
        internal abstract BatchLeaf<T> AssumeLeaf();
        internal abstract BatchBranchN<T> AssumeBranchN();
        internal abstract BatchBranch2<T> AssumeBranch2();
        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}