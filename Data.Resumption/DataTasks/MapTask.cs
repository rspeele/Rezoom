using System;
using Microsoft.FSharp.Core;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a data task with its output mapped to another data type via a synchronous function.
    /// This is just a special case of BindTask, but is represented as its own type for clarity in stack traces.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    internal static class MapTask<TIn, TOut>
    {
        private static StepState<TOut> Step(DataTask<TIn> bound, Func<TIn, TOut> mapping)
        {
            var state = bound.Step();
            return state.Pending != null
                ? StepState.Pending(state.Pending.Map(next => Create(next, mapping)))
                : StepState.Result(mapping(state.Result));
        }

        public static DataTask<TOut> Create(DataTask<TIn> bound, Func<TIn, TOut> mapping)
            => new DataTask<TOut>(() => Step(bound, mapping));

        private static StepState<TOut> Step(DataTask<TIn> bound, FSharpFunc<TIn, TOut> mapping)
        {
            var state = bound.Step();
            return state.Pending != null
                ? StepState.Pending(state.Pending.Map(next => Create(next, mapping)))
                : StepState.Result(mapping.Invoke(state.Result));
        }

        public static DataTask<TOut> Create(DataTask<TIn> bound, FSharpFunc<TIn, TOut> mapping)
            => new DataTask<TOut>(() => Step(bound, mapping));
    }
}
