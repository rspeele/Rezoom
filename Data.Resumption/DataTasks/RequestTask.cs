namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Wraps a single IDataRequest up as an IDataTask.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal static class RequestTask<TResult>
    {
        public static IDataTask<TResult> Create(IDataRequest<TResult> dataRequest)
        {
            var pending = new RequestsPending<TResult>
                ( new BatchLeaf<IDataRequest>(dataRequest)
                , batch =>
                {
                    var success = (TResult)batch.AssumeLeaf().Element.Success;
                    return new IDataTask<TResult>(success);
                });
            return new IDataTask<TResult>(pending);
        }
    }
}