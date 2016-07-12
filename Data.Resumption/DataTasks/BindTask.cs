using System;

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
            return state.Match(pending =>
            {
                var mapped = pending.Map(next => Create(next, continuation));
                return StepState.Pending(mapped);
            }, result => continuation(result).Step());
        }

        public static DataTask<TResult> Create
            (DataTask<TPending> bound, Func<TPending, DataTask<TResult>> continuation)
            => new DataTask<TResult>(() => Step(bound, continuation));
    }
}