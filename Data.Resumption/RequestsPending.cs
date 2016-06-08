using System;

namespace Data.Resumption
{
    public struct RequestsPending<TResult>
    {
        public RequestsPending
            ( Batch<IDataRequest> requests
            , Func<Batch<object>, IDataTask<TResult>> resume
            )
        {
            Requests = requests;
            Resume = resume;
        }

        public Batch<IDataRequest> Requests { get; }
        public Func<Batch<object>, IDataTask<TResult>> Resume { get; }

        public RequestsPending<TOutResult> Map<TOutResult>(Func<IDataTask<TResult>, IDataTask<TOutResult>> mapping)
        {
            var res = Resume;
            return new RequestsPending<TOutResult>(Requests, response => mapping(res(response)));
        }
    }
}