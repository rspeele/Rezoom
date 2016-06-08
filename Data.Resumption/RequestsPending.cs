using System;

namespace Data.Resumption
{
    public struct RequestsPending<TResult>
    {
        public RequestsPending
            ( Batch<IDataRequest> requests
            , Func<Batch<object>, IDataTask<TResult>> resume
            , Func<Batch<Exception>, IDataTask<TResult>> onException
            )
        {
            Requests = requests;
            Resume = resume;
            OnException = onException;
        }

        public Batch<IDataRequest> Requests { get; }
        public Func<Batch<object>, IDataTask<TResult>> Resume { get; }
        /// <summary>
        /// Attempt to handle exceptions.
        /// If exceptions are not handled, returns null for the next task.
        /// </summary>
        public Func<Batch<Exception>, IDataTask<TResult>> OnException { get; }

        public RequestsPending<TOutResult> Map<TOutResult>(Func<IDataTask<TResult>, IDataTask<TOutResult>> mapping)
        {
            Func<Batch<object>, IDataTask<TOutResult>> resume;
            {
                var res = Resume;
                resume = response => mapping(res(response));
            }
            Func<Batch<Exception>, IDataTask<TOutResult>> onException;
            {
                var exc = OnException;
                onException = ex =>
                {
                    var handled = exc(ex);
                    return handled != null ? mapping(handled) : null;
                };
            }
            return new RequestsPending<TOutResult>(Requests, resume, onException);
        }
    }
}