using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a chain of data tasks where the result of the first determines what the second
    /// will be (AKA a monadic bind).
    /// </summary>
    /// <typeparam name="TPending"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    internal class BindTask<TPending, TResult> : IDataTask<TResult>
    {
        private readonly IDataTask<TPending> _bound;
        private readonly Func<TPending, IDataTask<TResult>> _continuation;

        public BindTask(IDataTask<TPending> bound, Func<TPending, IDataTask<TResult>> continuation)
        {
            _bound = bound;
            _continuation = continuation;
        }

        public StepState<TResult> Step()
        {
            var state = _bound.Step();
            return state.Match(pending =>
            {
                var mapped = pending.Map(next => new BindTask<TPending, TResult>(next, _continuation));
                return StepState.Pending(mapped);
            }, result => _continuation(result).Step());
        }
    }
}