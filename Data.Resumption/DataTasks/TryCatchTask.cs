using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a data task with a "catch" block that attempts to handle exceptions encountered during the task's execution.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TryCatchTask<T> : IDataTask<T>
    {
        private readonly IDataTask<T> _wrapped;
        private readonly Func<Exception, IDataTask<T>> _exceptionHandler; // may rethrow the exception

        public TryCatchTask(IDataTask<T> wrapped, Func<Exception, IDataTask<T>> exceptionHandler)
        {
            _wrapped = wrapped;
            _exceptionHandler = exceptionHandler;
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
                            {
                                return new TryCatchTask<T>(pending.Resume(response), _exceptionHandler);
                            }
                            catch (Exception ex)
                            {
                                return _exceptionHandler(ex);
                            }
                        }))
                    , StepState.Result);
            }
            catch (Exception ex)
            {
                return _exceptionHandler(ex).Step();
            }
        }
    }
}
