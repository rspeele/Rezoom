using System;
using Microsoft.FSharp.Core;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Implements a chain of data tasks where the result of the first determines what the second
    /// will be (AKA a monadic bind).
    /// </summary>
    /// <typeparam name="TPending"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    internal static class BindTask<TPending, TResult>
    {
        private static StepState<TResult> Step
            (DataTask<TPending> bound, Func<TPending, DataTask<TResult>> continuation)
        {
            var state = bound.Step();
            return state.Pending == null
                ? continuation(state.Result).Step()
                : StepState.Pending(state.Pending.Map(next => Create(next, continuation)));
        }

        public static DataTask<TResult> Create
            (DataTask<TPending> bound, Func<TPending, DataTask<TResult>> continuation)
            => new DataTask<TResult>(() => Step(bound, continuation));

        private static StepState<TResult> Step
            (DataTask<TPending> bound, FSharpFunc<TPending, DataTask<TResult>> continuation)
        {
            var state = bound.Step();
            return state.Pending == null
                ? continuation.Invoke(state.Result).Step()
                : StepState.Pending(state.Pending.Map(next => Create(next, continuation)));
        }

        public static DataTask<TResult> Create
            (DataTask<TPending> bound, FSharpFunc<TPending, DataTask<TResult>> continuation)
            => new DataTask<TResult>(() => Step(bound, continuation));
    }
}