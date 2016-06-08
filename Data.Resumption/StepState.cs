using System;

namespace Data.Resumption
{
    public abstract class StepState<TResult>
    {
        public abstract T Visit<T>(Func<RequestsPending<TResult>, T> onPending, Func<TResult, T> onResult);
    }

    internal class PendingStepState<TResult> : StepState<TResult>
    {
        private readonly RequestsPending<TResult> _pending;

        public PendingStepState(RequestsPending<TResult> pending)
        {
            _pending = pending;
        }

        public override T Visit<T>(Func<RequestsPending<TResult>, T> onPending, Func<TResult, T> onResult)
            => onPending(_pending);
    }

    internal class ResultStepState<TResult> : StepState<TResult>
    {
        private readonly TResult _result;

        public ResultStepState(TResult result)
        {
            _result = result;
        }

        public override T Visit<T>(Func<RequestsPending<TResult>, T> onPending, Func<TResult, T> onResult)
            => onResult(_result);
    }

    public static class StepState
    {
        public static StepState<TResult> Result<TResult>(TResult result) => new ResultStepState<TResult>(result);
        public static StepState<TResult> Pending<TResult>(RequestsPending<TResult> pending) => new PendingStepState<TResult>(pending);
    }
}