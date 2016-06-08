using System;
using System.Runtime.ExceptionServices;

namespace Data.Resumption
{
    internal static class InternalExtensions
    {
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
