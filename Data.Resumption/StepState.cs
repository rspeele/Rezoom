using System;

namespace Data.Resumption
{
    public abstract class StepState<TResult>
    {
        public abstract T Match<T>(Func<RequestsPending<TResult>, T> onPending, Func<TResult, T> onResult);
    }

    public static class StepState
    {
        public static StepState<TResult> Result<TResult>(TResult result) => new ResultStepState<TResult>(result);
        public static StepState<TResult> Pending<TResult>(RequestsPending<TResult> pending) => new StepStatePending<TResult>(pending);
    }
}