namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Wraps a single IDataRequest up as an IDataTask.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal class RequestTask<TResult> : IDataTask<TResult>
    {
        private readonly RequestsPending<TResult> _pending;

        public RequestTask(IDataRequest<TResult> dataRequest)
        {
            _pending = new RequestsPending<TResult>
                ( new BatchLeaf<IDataRequest>(dataRequest)
                , batch =>
                {
                    var response = (TResult)batch.AssumeLeaf().Element;
                    return new ReturnTask<TResult>(response);
                });
        }

        public StepState<TResult> Step() => StepState.Pending(_pending);
    }
}