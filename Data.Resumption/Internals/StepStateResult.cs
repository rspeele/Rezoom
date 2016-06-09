using System;

namespace Data.Resumption
{
    internal class ResultStepState<TResult> : StepState<TResult>
    {
        private readonly TResult _result;

        public ResultStepState(TResult result)
        {
            _result = result;
        }

        public override T Match<T>(Func<RequestsPending<TResult>, T> onPending, Func<TResult, T> onResult)
            => onResult(_result);
    }
}