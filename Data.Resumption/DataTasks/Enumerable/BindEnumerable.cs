using System;

namespace Data.Resumption.DataTasks.Enumerable
{
    internal class BindEnumerable<TPending, TElem> : IDataEnumerable<TElem>
    {
        private readonly IDataTask<TPending> _bound;
        private readonly Func<TPending, IDataEnumerable<TElem>> _continuation;

        public BindEnumerable
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
