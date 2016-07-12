using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a data task with a "catch" block that attempts to handle exceptions encountered during the task's execution.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class TryCatchTask<T>
    {
        public static IDataTask<T> Create(IDataTask<T> wrapped, Func<Exception, IDataTask<T>> exceptionHandler)
            => new IDataTask<T>(() => Step(wrapped, exceptionHandler));

        internal static StepState<T> Step(IDataTask<T> wrapped, Func<Exception, IDataTask<T>> exceptionHandler)
        {
            try
            {
                return wrapped.Step().Match
                    ( pending =>
                        StepState.Pending(new RequestsPending<T>(pending.Requests, response =>
                        {
                            try
                            {
                                return Create(pending.Resume(response), exceptionHandler);
                            }
                            catch (DataTaskAbortException)
                            {
                                throw; // we don't allow catching these
                            }
                            catch (Exception ex)
                            {
                                return exceptionHandler(ex);
                            }
                        }))
                    , StepState.Result);
            }
            catch (DataTaskAbortException)
            {
                throw; // we don't allow catching these
            }
            catch (Exception ex)
            {
                return exceptionHandler(ex).Step();
            }
        }
    }
}
