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
            (IDataTask<TPending> bound, Func<TPending, IDataTask<TResult>> continuation)
        {
            var state = bound.Step();
            return state.Match(pending =>
            {
                var mapped = pending.Map(next => Create(next, continuation));
                return StepState.Pending(mapped);
            }, result => continuation(result).Step());
        }

        public static IDataTask<TResult> Create
            (IDataTask<TPending> bound, Func<TPending, IDataTask<TResult>> continuation)
            => new IDataTask<TResult>(() => Step(bound, continuation));
    }
}