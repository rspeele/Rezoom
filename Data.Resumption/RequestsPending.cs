using System;

namespace Data.Resumption
{
    public struct RequestsPending<TResult>
    {
        public RequestsPending
            ( Batch<IDataRequest> requests
            , Func<Batch<SuccessOrException>, IDataTask<TResult>> resume
            )
        {
            Requests = requests;
            Resume = resume;
        }

        public Batch<IDataRequest> Requests { get; }
        public Func<Batch<SuccessOrException>, IDataTask<TResult>> Resume { get; }

        public RequestsPending<TOutResult> Map<TOutResult>
            (Func<IDataTask<TResult>, IDataTask<TOutResult>> mapping)
        {
            var res = Resume;
            return new RequestsPending<TOutResult>
                (Requests, response => mapping(res(response)));
        }
    }
}