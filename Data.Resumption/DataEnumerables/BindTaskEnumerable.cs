﻿using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents the binding of a data task's result to the following
    /// yields within a data enumerable.
    /// </summary>
    /// <typeparam name="TPending"></typeparam>
    /// <typeparam name="TElement"></typeparam>
    internal class BindTaskEnumerable<TPending, TElement> : IDataEnumerable<TElement>
    {
        private readonly DataTask<TPending> _bound;
        private readonly Func<TPending, IDataEnumerable<TElement>> _continuation;

        public BindTaskEnumerable
            ( DataTask<TPending> bound
            , Func<TPending, IDataEnumerable<TElement>> continuation
            )
        {
            _bound = bound;
            _continuation = continuation;
        }

        private class BindTaskEnumerator : IDataEnumerator<TElement>
        {
            private DataTask<TPending> _bound;
            private Func<TPending, IDataEnumerable<TElement>> _continuation;
            private IDataEnumerator<TElement> _wrapped;

            public BindTaskEnumerator(DataTask<TPending> bound, Func<TPending, IDataEnumerable<TElement>> continuation)
            {
                _bound = bound;
                _continuation = continuation;
            }

            public DataTask<DataTaskYield<TElement>> MoveNext()
            {
                if (_wrapped != null) return _wrapped.MoveNext();
                return _bound.Bind(pending =>
                {
                    _wrapped = _continuation(pending).GetEnumerator();
                    _continuation = null;
                    return _wrapped.MoveNext();
                });
            }

            public void Dispose() => _wrapped?.Dispose();
        }

        public IDataEnumerator<TElement> GetEnumerator()
            => new BindTaskEnumerator(_bound, _continuation);
    }
}
