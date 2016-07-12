using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a data task with a "finally" block that will run regardless of whether an exception occurs during task execution.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class TryFinallyTask<T>
    {
        public static DataTask<T> Create(DataTask<T> wrapped, Action onExit)
            => new DataTask<T>(() => Step(wrapped, onExit));

        internal static StepState<T> Step(DataTask<T> wrapped, Action onExit)
        {
            try
            {
                return wrapped.Step().Match
                    ( pending =>
                        StepState.Pending(new RequestsPending<T>(pending.Requests, response =>
                        {
                            try
                            { // if we can resume successfully, our next step needs to be wrapped too
                                return Create(pending.Resume(response), onExit);
                            }
                            catch
                            {
                                onExit();
                                throw;
                            }
                        }))
                        , result =>
                        { // if we got all the way to the end without errors, we still need to run the `finally` code
                            onExit();
                            return StepState.Result(result);
                        }
                    );
            }
            catch
            {
                onExit();
                throw;
            }
        }
    }
}