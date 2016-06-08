using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a data task with its output mapped to another data type via a synchronous function.
    /// This is just a special case of BindTask, but is represented as its own type for clarity in stack traces.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    public class MapTask<TIn, TOut> : IDataTask<TOut>
    {
        private readonly IDataTask<TIn> _bound;
        private readonly Func<TIn, TOut> _mapping;

        public MapTask(IDataTask<TIn> bound, Func<TIn, TOut> mapping)
        {
            _bound = bound;
            _mapping = mapping;
        }

        public StepState<TOut> Step()
        {
            var state = _bound.Step();
            return state.Match(pending =>
            {
                var mapped = pending.Map(next => new MapTask<TIn, TOut>(next, _mapping));
                return StepState.Pending(mapped);
            }, result => StepState.Result(_mapping(result)));
        }
    }
}
