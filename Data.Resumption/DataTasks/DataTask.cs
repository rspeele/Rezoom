using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Data.Resumption.DataTasks
{
    public static class DataTask
    {
        public static IDataTask<TOut> Select<TIn, TOut>(this IDataTask<TIn> bound, Func<TIn, TOut> mapping)
            => new MapTask<TIn, TOut>(bound, mapping);
        public static IDataTask<TOut> SelectMany<TPending, TOut>(this IDataTask<TPending> bound, Func<TPending, IDataTask<TOut>> continuation)
            => new BindTask<TPending, TOut>(bound, continuation);
        public static IDataTask<T> Return<T>(T value) => new ReturnTask<T>(value);
        public static IDataTask<TOut> Apply<T, TOut>(this IDataTask<Func<T, TOut>> functionTask, IDataTask<T> inputTask)
            => new ApplyTask<T, TOut>(functionTask, inputTask);
        public static IDataTask<TSum> Sum<T, TSum>(this IEnumerable<IDataTask<T>> tasks, TSum initial, Func<TSum, T, TSum> add)
            => new SumTask<T, TSum>(tasks, initial, add);

        private static void Abort<T>(Func<StepState<T>> step, Exception causeOfAbortion)
        {
            try
            {
                step().Match(pending => pending.Resume(new BatchAbortion<SuccessOrException>()), _ => null);
            }
            catch (DataTaskAbortException) // it's normal for this to happen
            {
            }
            catch (Exception nextEx)
            {
                throw new AggregateException(causeOfAbortion, nextEx);
            }
            ExceptionDispatchInfo.Capture(causeOfAbortion).Throw(); // this should rethrow the exception with its original trace
            throw causeOfAbortion; // should be impossible to get here, but if we did, the best thing to do is rethrow normally
        }
        internal static void Abort<T>(this IDataTask<T> taskToAbort, Exception causeOfAbortion) => Abort(taskToAbort.Step, causeOfAbortion);
        internal static void Abort<T>(this StepState<T> stepToAbort, Exception causeOfAbortion) => Abort(() => stepToAbort, causeOfAbortion);
    }
}
