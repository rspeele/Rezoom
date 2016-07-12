using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace Data.Resumption
{
    internal static class InternalExtensions
    {
        public static StepState<TResult> Step<TResult>(this IDataTask<TResult> task)
            => IDataTask<TResult>.InternalStep(task);

        public static Exception Aggregate(this ICollection<Exception> exceptions)
            => exceptions.Count == 1 ? exceptions.First() : new AggregateException(exceptions);

        /// <summary>
        /// Abort all the tasks paused on <see cref="steps"/>, with <paramref name="causeOfAbortion"/> as the reason.
        /// </summary>
        /// <remarks>
        /// This works by resuming the tasks with an invalid batch of results.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="steps"></param>
        /// <param name="causeOfAbortion"></param>
        private static void AbortMany<T>(IEnumerable<Func<StepState<T>>> steps, Exception causeOfAbortion)
        {
            var subExceptions = new List<Exception>();
            foreach (var step in steps)
            {
                try
                {
                    step().Match
                        (pending => pending.Resume(new BatchAbortion<SuccessOrException>())
                        , _ => default(IDataTask<T>));
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
                throw new AggregateException(causeOfAbortion, subExceptions.Aggregate());
            }
            // this should rethrow the exception with its original trace
            ExceptionDispatchInfo.Capture(causeOfAbortion).Throw();
            // should be impossible to get here, but if we did, the best thing to do is rethrow normally
            throw causeOfAbortion;
        }
        /// <summary>
        /// Abort all the tasks in <paramref name="taskToAbort"/>, with <paramref name="causeOfAbortion"/>
        /// as the reason.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="taskToAbort"></param>
        /// <param name="causeOfAbortion"></param>
        internal static void AbortMany<T>(this IEnumerable<IDataTask<T>> taskToAbort, Exception causeOfAbortion)
            => AbortMany(taskToAbort.Select(t => (Func<StepState<T>>)(() => t.Step())), causeOfAbortion);
        /// <summary>
        /// Abort all the tasks paused on <paramref name="stepToAbort"/>, with <paramref name="causeOfAbortion"/>
        /// as the reason.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stepToAbort"></param>
        /// <param name="causeOfAbortion"></param>
        internal static void AbortMany<T>(this IEnumerable<StepState<T>> stepToAbort, Exception causeOfAbortion)
            => AbortMany(stepToAbort.Select(s => (Func<StepState<T>>)(() => s)), causeOfAbortion);

        /// <summary>
        /// Abort <see cref="step"/> with <paramref name="causeOfAbortion"/> as the reason.
        /// </summary>
        /// <remarks>
        /// This works by resuming <see cref="step"/> with an invalid batch.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="step"></param>
        /// <param name="causeOfAbortion"></param>
        private static void Abort<T>(Func<StepState<T>> step, Exception causeOfAbortion)
        {
            try
            {
                step().Match(pending => pending.Resume
                    (new BatchAbortion<SuccessOrException>()), _ => default(IDataTask<T>));
            }
            catch (DataTaskAbortException) // it's normal for this to happen
            {
            }
            catch (Exception subEx)
            {
                throw new AggregateException(causeOfAbortion, subEx);
            }
            // this should rethrow the exception with its original trace
            ExceptionDispatchInfo.Capture(causeOfAbortion).Throw();
            // should be impossible to get here, but if we did, the best thing to do is rethrow normally
            throw causeOfAbortion;
        }
        internal static void Abort<T>(this IDataTask<T> taskToAbort, Exception causeOfAbortion)
            => Abort(() => taskToAbort.Step(), causeOfAbortion);
        internal static void Abort<T>(this StepState<T> stepToAbort, Exception causeOfAbortion)
            => Abort(() => stepToAbort, causeOfAbortion);
    }
}
