using System;

namespace Data.Resumption
{
    internal class StepStatePending<TResult> : StepState<TResult>
    {
        private readonly RequestsPending<TResult> _pending;

        public StepStatePending(RequestsPending<TResult> pending)
        {
            _pending = pending;
        }

        public override T Match<T>(Func<RequestsPending<TResult>, T> onPending, Func<TResult, T> onResult)
            => onPending(_pending);
    }
}