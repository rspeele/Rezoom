using System;

namespace Data.Resumption
{
    /// <summary>
    /// Represents the state of execution of an <see cref="IDataTask{TResult}"/>.
    /// May either be the final result of the task, or a batch of pending requests.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public abstract class StepState<TResult>
    {
        /// <summary>
        /// Pattern-match against the two possible cases of a <see cref="StepState{TResult}"/> by providing
        /// a function to handle each each case.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="onPending">The function to be called if this is a pending request state.</param>
        /// <param name="onResult">The function to be called if this is a result state.</param>
        /// <returns></returns>
        public abstract T Match<T>
            ( Func<RequestsPending<TResult>, T> onPending
            , Func<TResult, T> onResult
            );
    }

    internal static class StepState
    {
        /// <summary>
        /// Create a <see cref="StepState{TResult}"/> which represents the final <paramref name="result"/> of a task.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        public static StepState<TResult> Result<TResult>(TResult result)
            => new ResultStepState<TResult>(result);
        /// <summary>
        /// Create a <see cref="StepState{TResult}"/> which represents pending data requests.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="pending"></param>
        /// <returns></returns>
        public static StepState<TResult> Pending<TResult>(RequestsPending<TResult> pending)
            => new StepStatePending<TResult>(pending);
    }
}