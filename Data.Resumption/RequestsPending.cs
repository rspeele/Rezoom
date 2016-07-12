using System;

namespace Data.Resumption
{
    /// <summary>
    /// Represents a batch of pending <see cref="IDataRequest"/>s paired with a function to resume execution with an
    /// <see cref="DataTask{TResult}"/> based on the results of those requests.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class RequestsPending<TResult>
    {
        public RequestsPending
            ( Batch<IDataRequest> requests
            , Func<Batch<SuccessOrException>, DataTask<TResult>> resume
            )
        {
            Requests = requests;
            Resume = resume;
        }

        /// <summary>
        /// The batch of <see cref="IDataRequest"/>s that the task is waiting on.
        /// </summary>
        public Batch<IDataRequest> Requests { get; }
        /// <summary>
        /// A function to get the continuation <see cref="DataTask{TResult}"/> based on
        /// a batch of <see cref="SuccessOrException"/>s representing the results of <see cref="Requests"/>.
        /// </summary>
        public Func<Batch<SuccessOrException>, DataTask<TResult>> Resume { get; }

        /// <summary>
        /// Transform the continuation of this pending state.
        /// </summary>
        /// <remarks>
        /// The resulting <see cref="RequestsPending{TOutResult}"/> will have the same batch of requests,
        /// but its <see cref="Resume"/> continuation will be transformed by <paramref name="mapping"/>.
        /// </remarks>
        /// <typeparam name="TOutResult"></typeparam>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public RequestsPending<TOutResult> Map<TOutResult>
            (Func<DataTask<TResult>, DataTask<TOutResult>> mapping)
        {
            var res = Resume;
            return new RequestsPending<TOutResult>
                (Requests, response => mapping(res(response)));
        }
    }
}