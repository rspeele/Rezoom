using System;

namespace Data.Resumption.DataEnumerables
{
    /// <summary>
    /// Represents the binding of a data task's result to the following
    /// yields within a data enumerable.
    /// </summary>
    /// <typeparam name="TPending"></typeparam>
    /// <typeparam name="TElem"></typeparam>
    internal class BindTaskEnumerable<TPending, TElem> : IDataEnumerable<TElem>
    {
        private readonly IDataTask<TPending> _bound;
        private readonly Func<TPending, IDataEnumerable<TElem>> _continuation;

        public BindTaskEnumerable
            ( IDataTask<TPending> bound
            , Func<TPending, IDataEnumerable<TElem>> continuation
            )
        {
            _bound = bound;
            _continuation = continuation;
        }

        public IDataTask<DataTaskYield<TElem>?> Yield()
            => _bound.SelectMany(pending => _continuation(pending).Yield());
    }
}
