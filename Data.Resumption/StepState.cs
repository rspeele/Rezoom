using System;

namespace Data.Resumption
{
    /// <summary>
    /// Represents the state of execution of an <see cref="DataTask{TResult}"/>.
    /// May either be the final result of the task, or a batch of pending requests.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public struct StepState<TResult>
    {
        internal readonly TResult Result;
        internal readonly RequestsPending<TResult> Pending;
        internal StepState(TResult result)
        {
            Result = result;
            Pending = null;
        }
        internal StepState(RequestsPending<TResult> pending)
        {
            Pending = pending;
            Result = default(TResult);
        }

        /// <summary>
        /// Pattern-match against the two possible cases of a <see cref="StepState{TResult}"/> by providing
        /// a function to handle each each case.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="state"></param>
        /// <param name="onPending">The function to be called if this is a pending request state.</param>
        /// <param name="onResult">The function to be called if this is a result state.</param>
        /// <returns></returns>
        internal static T InternalMatch<T>
            ( StepState<TResult> state
            , Func<RequestsPending<TResult>, T> onPending
            , Func<TResult, T> onResult
            ) => state.Pending != null ? onPending(state.Pending) : onResult(state.Result);
    }

    internal static class StepState
    {
        public static T Match<T, TResult>
            ( this StepState<TResult> state
            , Func<RequestsPending<TResult>, T> onPending
            , Func<TResult, T> onResult
            ) => StepState<TResult>.InternalMatch(state, onPending, onResult);

        /// <summary>
        /// Create a <see cref="StepState{TResult}"/> which represents the final <paramref name="result"/> of a task.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        public static StepState<TResult> Result<TResult>(TResult result)
            => new StepState<TResult>(result);
        /// <summary>
        /// Create a <see cref="StepState{TResult}"/> which represents pending data requests.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="pending"></param>
        /// <returns></returns>
        public static StepState<TResult> Pending<TResult>(RequestsPending<TResult> pending)
            => new StepState<TResult>(pending);
    }
}