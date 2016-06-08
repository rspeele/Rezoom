namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a data task that simply returns a known value.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal class ReturnTask<TResult> : IDataTask<TResult>
    {
        private readonly TResult _result;

        public ReturnTask(TResult result)
        {
            _result = result;
        }

        public StepState<TResult> Step() => StepState.Result(_result);
    }
}
