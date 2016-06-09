using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a data task with a "finally" block that will run regardless of whether an exception occurs during task execution.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TryFinallyTask<T> : IDataTask<T>
    {
        private readonly IDataTask<T> _wrapped;
        private readonly Action _onExit;

        public TryFinallyTask(IDataTask<T> wrapped, Action onExit)
        {
            _wrapped = wrapped;
            _onExit = onExit;
        }

        public StepState<T> Step()
        {
            try
            {
                return _wrapped.Step().Match
                    ( pending =>
                        StepState.Pending(new RequestsPending<T>(pending.Requests, response =>
                        {
                            try
                            { // if we can resume successfully, our next step needs to be wrapped too
                                return new TryFinallyTask<T>(pending.Resume(response), _onExit);
                            }
                            catch
                            {
                                _onExit();
                                throw;
                            }
                        }))
                        , result =>
                        { // if we got all the way to the end without errors, we still need to run the `finally` code
                            _onExit();
                            return StepState.Result(result);
                        }
                    );
            }
            catch
            {
                _onExit();
                throw;
            }
        }
    }
}