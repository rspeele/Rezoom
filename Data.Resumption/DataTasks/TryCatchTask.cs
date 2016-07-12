using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a data task with a "catch" block that attempts to handle exceptions encountered during the task's execution.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class TryCatchTask<T>
    {
        public static DataTask<T> Create(DataTask<T> wrapped, Func<Exception, DataTask<T>> exceptionHandler)
            => new DataTask<T>(() => Step(wrapped, exceptionHandler));

        internal static StepState<T> Step(DataTask<T> wrapped, Func<Exception, DataTask<T>> exceptionHandler)
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
