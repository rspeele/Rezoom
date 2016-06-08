using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace Data.Resumption
{
    internal static class InternalExtensions
    {
        private static void AbortMany<T>(IEnumerable<Func<StepState<T>>> steps, Exception causeOfAbortion)
        {
            var subExceptions = new List<Exception>();
            foreach (var step in steps)
            {
                try
                {
                    step().Match(pending => pending.Resume(new BatchAbortion<SuccessOrException>()), _ => null);
                }
                catch (DataTaskAbortException) // it's normal for this to happen
                {
                }
                catch (Exception subEx)
                {
                    subExceptions.Add(subEx);
                }
            }
            if (subExceptions.Any())
            {
                throw new AggregateException(causeOfAbortion, new AggregateException(subExceptions));
            }
            ExceptionDispatchInfo.Capture(causeOfAbortion).Throw(); // this should rethrow the exception with its original trace
            throw causeOfAbortion; // should be impossible to get here, but if we did, the best thing to do is rethrow normally
        }
        internal static void AbortMany<T>(this IEnumerable<IDataTask<T>> taskToAbort, Exception causeOfAbortion)
            => AbortMany(taskToAbort.Select(t => (Func<StepState<T>>)t.Step), causeOfAbortion);
        internal static void AbortMany<T>(this IEnumerable<StepState<T>> stepToAbort, Exception causeOfAbortion)
            => AbortMany(stepToAbort.Select(s => (Func<StepState<T>>)(() => s)), causeOfAbortion);
        private static void Abort<T>(Func<StepState<T>> step, Exception causeOfAbortion)
        {
            try
            {
                step().Match(pending => pending.Resume(new BatchAbortion<SuccessOrException>()), _ => null);
            }
            catch (DataTaskAbortException) // it's normal for this to happen
            {
            }
            catch (Exception subEx)
            {
                throw new AggregateException(causeOfAbortion, subEx);
            }
            ExceptionDispatchInfo.Capture(causeOfAbortion).Throw(); // this should rethrow the exception with its original trace
            throw causeOfAbortion; // should be impossible to get here, but if we did, the best thing to do is rethrow normally
        }
        internal static void Abort<T>(this IDataTask<T> taskToAbort, Exception causeOfAbortion) => Abort(taskToAbort.Step, causeOfAbortion);
        internal static void Abort<T>(this StepState<T> stepToAbort, Exception causeOfAbortion) => Abort(() => stepToAbort, causeOfAbortion);
    }
}
