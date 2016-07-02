using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Resumption.DataRequests;
using Data.Resumption.DataTasks;

namespace Data.Resumption
{
    /// <summary>
    /// Contains useful extension methods for creating and composing <see cref="IDataTask{TResult}"/>s.
    /// </summary>
    public static class DataTask
    {
        /// <summary>
        /// Convert a CLR <see cref="Task"/> to an <see cref="IDataRequest{TResponse}"/>.
        /// </summary>
        /// <param name="asyncTask"></param>
        /// <returns></returns>
        public static IDataRequest<object> ToDataRequest(this Func<Task> asyncTask)
            => new OpaqueAsyncDataRequest<object>(() => asyncTask().ContinueWith(_ => (object)null));

        /// <summary>
        /// Convert a CLR <see cref="Task{T}"/> to an <see cref="IDataRequest{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asyncTask"></param>
        /// <returns></returns>
        public static IDataRequest<T> ToDataRequest<T>(this Func<Task<T>> asyncTask)
            => new OpaqueAsyncDataRequest<T>(asyncTask);

        /// <summary>
        /// Convert a CLR <see cref="Task"/> to an <see cref="IDataTask{TResult}"/>.
        /// </summary>
        /// <param name="asyncTask"></param>
        /// <returns></returns>
        public static IDataTask<object> ToDataTask(this Func<Task> asyncTask)
            => asyncTask.ToDataRequest().ToDataTask();

        /// <summary>
        /// Convert a CLR <see cref="Task{T}"/> to an <see cref="IDataTask{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asyncTask"></param>
        /// <returns></returns>
        public static IDataTask<T> ToDataTask<T>(this Func<Task<T>> asyncTask)
            => asyncTask.ToDataRequest().ToDataTask();

        /// <summary>
        /// Convert an <see cref="IDataRequest{T}"/> to a <see cref="IDataTask{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataRequest"></param>
        /// <returns></returns>
        public static IDataTask<T> ToDataTask<T>(this IDataRequest<T> dataRequest)
            => new RequestTask<T>(dataRequest);

        /// <summary>
        /// Map a synchronous function <paramref name="mapping"/> over the result of an
        /// <see cref="IDataTask{TIn}"/> to obtain an <see cref="IDataTask{TOut}"/>.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="bound"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public static IDataTask<TOut> Select<TIn, TOut>
            (this IDataTask<TIn> bound, Func<TIn, TOut> mapping)
            => new MapTask<TIn, TOut>(bound, mapping);

        /// <summary>
        /// Chain a dependent task onto an <see cref="IDataTask{TPending}"/> to obtain an <see cref="IDataTask{TOut}"/>.
        /// <paramref name="continuation"/> uses the result of the <paramref name="bound"/> task to decide
        /// what task to perform next.
        /// </summary>
        /// <typeparam name="TPending"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="bound"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static IDataTask<TOut> Bind<TPending, TOut>
            (this IDataTask<TPending> bound, Func<TPending, IDataTask<TOut>> continuation)
            => new BindTask<TPending, TOut>(bound, continuation);

        /// <summary>
        /// Alias for <see cref="Bind{TPending,TOut}"/> used in LINQ expression syntax.
        /// </summary>
        /// <example>
        /// The following code is syntax sugar:
        /// <code>
        /// from x in taskX
        /// from y in taskY(x)
        /// select x + y
        /// </code>
        /// It desugars to:
        /// <code>
        /// taskX.SelectMany(x => taskY(x).Select(y => x + y))
        /// </code>
        /// </example>
        /// <typeparam name="TPending"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="bound"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static IDataTask<TOut> SelectMany<TPending, TOut>
            (this IDataTask<TPending> bound, Func<TPending, IDataTask<TOut>> continuation)
            => new BindTask<TPending, TOut>(bound, continuation);

        /// <summary>
        /// Create a finished <see cref="IDataTask{TResult}"/> whose result is <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IDataTask<T> Return<T>(T value) => new ReturnTask<T>(value);

        /// <summary>
        /// Compose two <see cref="IDataTask{T}"/>s into one, creating its result by by applying
        /// the function returned by the first to the input returned by the second.
        /// </summary>
        /// <remarks>
        /// Because the two <see cref="IDataTask{T}"/>s are independent, they will run concurrently.
        /// If you would like them to run sequentially instead (perhaps to ensure that side effects
        /// occur in the correct order), you should use <see cref="Bind{TPending,TOut}"/>.
        /// 
        /// If one of the two composed tasks throws an exception and does not catch it, and the other task
        /// is still progressing normally, the surviving task will be canceled by resuming with a
        /// <see cref="DataTaskAbortException"/>. This type of exception cannot be caught, but it will
        /// cause pending <c>finally</c> blocks to run.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="functionTask"></param>
        /// <param name="inputTask"></param>
        /// <returns></returns>
        public static IDataTask<TOut> Apply<T, TOut>
            (this IDataTask<Func<T, TOut>> functionTask, IDataTask<T> inputTask)
            => new ApplyTask<T, TOut>(functionTask, inputTask);

        /// <summary>
        /// Combine an <see cref="IDataTask{TLeft}"/> and an <see cref="IDataTask{TRight}"/> into
        /// an <see cref="IDataTask{TOut}"/> using the function <paramref name="zipper"/>.
        /// </summary>
        /// <remarks>
        /// The two tasks are combined using <see cref="Apply{T,TOut}"/>, so they run concurrently.
        /// </remarks>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="zipper"></param>
        /// <returns></returns>
        public static IDataTask<TOut> Zip<TLeft, TRight, TOut>
            (this IDataTask<TLeft> left, IDataTask<TRight> right, Func<TLeft, TRight, TOut> zipper)
            => left
                .Select(lf => (Func<TRight, TOut>)(rt => zipper(lf, rt)))
                .Apply(right);

        /// <summary>
        /// Combine <paramref name="tasks"/> to run concurrently, accumulating their results in completion order
        /// using the <paramref name="add"/> function, starting with the <paramref name="initial"/> accumulator value.
        /// </summary>
        /// <remarks>
        /// The sequence must not be infinite, since it must be fully iterated to start stepping the tasks.
        /// 
        /// Callers should not rely on the order of <paramref name="add"/> operations, since it depends on the
        /// number of steps each input task takes.
        /// 
        /// If any of the composed tasks throws an exception and does not catch it, the surviving tasks will be
        /// canceled by resuming with a <see cref="DataTaskAbortException"/>.
        /// This type of exception cannot be caught, but it will cause pending <c>finally</c> blocks to run.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TSum"></typeparam>
        /// <param name="tasks"></param>
        /// <param name="initial"></param>
        /// <param name="add"></param>
        /// <returns></returns>
        public static IDataTask<TSum> Sum<T, TSum>
            (this IEnumerable<IDataTask<T>> tasks, TSum initial, Func<TSum, T, TSum> add)
            => new SumTask<T, TSum>(tasks, initial, add);

        /// <summary>
        /// Wrap an <see cref="IDataTask{T}"/> with an exception handler, which defines the task
        /// to fall back to in the event of an exception.
        /// </summary>
        /// <remarks>
        /// This catches exceptions that occur while executing the <see cref="IDataRequest"/>s generated by
        /// the <see cref="IDataTask{T}"/>, as well as exceptions thrown within calls to
        /// <see cref="IDataTask{T}.Step"/>.
        /// 
        /// <paramref name="exceptionHandler"/> should re-raise the given exception if it cannot handle it.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        public static IDataTask<T> TryCatch<T>
            (this IDataTask<T> wrapped, Func<Exception, IDataTask<T>> exceptionHandler)
            => new TryCatchTask<T>(wrapped, exceptionHandler);

        /// <summary>
        /// Wrap an <see cref="IDataTask{T}"/> with an exception handler, which defines the task
        /// to fall back to in the event of an exception.
        /// </summary>
        /// <remarks>
        /// This catches exceptions that occur while executing the <see cref="IDataRequest"/>s generated by
        /// the <see cref="IDataTask{T}"/>, as well as exceptions thrown within calls to
        /// <see cref="IDataTask{T}.Step"/>, and exceptions thrown in the given <paramref name="wrapped"/> function.
        /// 
        /// <paramref name="exceptionHandler"/> should re-raise the given exception if it cannot handle it.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        public static IDataTask<T> TryCatch<T>
            (this Func<IDataTask<T>> wrapped, Func<Exception, IDataTask<T>> exceptionHandler)
        {
            try
            {
                return wrapped().TryCatch(exceptionHandler);
            }
            catch (Exception ex)
            {
                return exceptionHandler(ex);
            }
        }

        /// <summary>
        /// Wrap an <see cref="IDataTask{T}"/> with a completion action, which executes when <paramref name="wrapped"/>
        /// finishes regardless of whether <paramref name="wrapped"/> produced a result, failed with an exception,
        /// or was cancelled due to an exception thrown from a concurrently applied task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="onExit"></param>
        /// <returns></returns>
        public static IDataTask<T> TryFinally<T>(this IDataTask<T> wrapped, Action onExit)
            => new TryFinallyTask<T>(wrapped, onExit);

        /// <summary>
        /// Wrap an <see cref="IDataTask{T}"/> with a completion action, which executes when <paramref name="wrapped"/>
        /// finishes regardless of whether <paramref name="wrapped"/> produced a result, failed with an exception,
        /// was cancelled due to an exception thrown from a concurrently applied task, or failed to be generated
        /// by the given function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="onExit"></param>
        /// <returns></returns>
        public static IDataTask<T> TryFinally<T>(this Func<IDataTask<T>> wrapped, Action onExit)
        {
            try
            {
                return wrapped().TryFinally(onExit);
            }
            catch
            {
                onExit();
                throw;
            }
        }

        /// <summary>
        /// Create a data task which asynchronously iterates over all elements of <paramref name="enumerable"/>,
        /// running <paramref name="iteration"/> on each element.
        /// </summary>
        /// <remarks>
        /// The <typeparamref name="TVoid"/> values returned from each iteration are ignored.
        /// The result of this task is always <c>default(TVoid)</c>.
        /// 
        /// <typeparamref name="TVoid"/> therefore does not matter, and would be <c>Unit</c> if that
        /// did not require a dependency on FSharp.Core.
        /// </remarks>
        /// <typeparam name="TElement"></typeparam>
        /// <typeparam name="TVoid"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="iteration"></param>
        /// <returns></returns>
        public static IDataTask<TVoid> ForEach<TElement, TVoid>
            (this IDataEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
            => new ForEachDataEnumerableTask<TElement, TVoid>(enumerable, iteration);

        /// <summary>
        /// Create a data task which iterates over all elements of <paramref name="enumerable"/>,
        /// running <paramref name="iteration"/> on each element.
        /// </summary>
        /// <remarks>
        /// The <typeparamref name="TVoid"/> values returned from each iteration are ignored.
        /// The result of this task is always <c>default(TVoid)</c>.
        /// 
        /// <typeparamref name="TVoid"/> therefore does not matter, and would be <c>Unit</c> if that
        /// did not require a dependency on FSharp.Core.
        /// </remarks>
        /// <typeparam name="TElement"></typeparam>
        /// <typeparam name="TVoid"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="iteration"></param>
        /// <returns></returns>
        public static IDataTask<TVoid> ForEach<TElement, TVoid>
            (this IEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
            => new ForEachEnumerableTask<TElement, TVoid>(enumerable, iteration);

        /// <summary>
        /// Create an <see cref="IDataTask{T}"/> which asynchronously iterates an <see cref="IDataEnumerable{T}"/>
        /// to produce a materialized list of its elements.
        /// </summary>
        /// <remarks>
        /// This is the typical way to get a normal collection type from an <see cref="IDataEnumerable{T}"/>.
        /// </remarks>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static IDataTask<List<TElement>> ToList<TElement>
            (this IDataEnumerable<TElement> enumerable)
        {
            var list = new List<TElement>();
            return new ForEachDataEnumerableTask<TElement, bool>
                (enumerable, e => { list.Add(e); return Return(false); }).Select(_ => list);
        }

        public static IDataTask<TAccum> Aggregate<TElement, TAccum>
            (this IDataEnumerable<TElement> enumerable, TAccum initial, Func<TAccum, TElement, TAccum> aggregator)
        {
            var current = initial;
            return new ForEachDataEnumerableTask<TElement, bool>
                (enumerable, e => { current = aggregator(current, e); return Return(false); }).Select(_ => current);
        }

        /// <summary>
        /// Use a <typeparamref name="TDisposable"/> in a <see cref="IDataTask{T}"/>, producing a task which will
        /// reliably dispose of the <typeparamref name="TDisposable"/> on completion regardless of exceptions.
        /// </summary>
        /// <typeparam name="TDisposable"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="getDisposable"></param>
        /// <param name="getBlock"></param>
        /// <returns></returns>
        public static IDataTask<T> Using<TDisposable, T>
            (this Func<TDisposable> getDisposable, Func<TDisposable, IDataTask<T>> getBlock)
            where TDisposable : IDisposable
        {
            var disposable = getDisposable();
            IDataTask<T> block;
            try
            {
                block = getBlock(disposable);
            }
            catch 
            {
                disposable.Dispose();
                throw;
            }
            return block.TryFinally(() => disposable.Dispose());
        }
    }
}
