using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Resumption.DataRequests;
using Data.Resumption.DataTasks;
using Microsoft.FSharp.Core;

namespace Data.Resumption
{
    /// <summary>
    /// Contains useful extension methods for creating and composing <see cref="DataTask{TResult}"/>s.
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
        /// Convert a CLR <see cref="Task"/> to an <see cref="DataTask{TResult}"/>.
        /// </summary>
        /// <param name="asyncTask"></param>
        /// <returns></returns>
        public static DataTask<object> ToDataTask(this Func<Task> asyncTask)
            => asyncTask.ToDataRequest().ToDataTask();

        /// <summary>
        /// Convert a CLR <see cref="Task{T}"/> to an <see cref="DataTask{TResult}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asyncTask"></param>
        /// <returns></returns>
        public static DataTask<T> ToDataTask<T>(this Func<Task<T>> asyncTask)
            => asyncTask.ToDataRequest().ToDataTask();

        /// <summary>
        /// Convert an <see cref="IDataRequest{T}"/> to a <see cref="DataTask{TResult}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataRequest"></param>
        /// <returns></returns>
        public static DataTask<T> ToDataTask<T>(this IDataRequest<T> dataRequest)
            => RequestTask<T>.Create(dataRequest);

        /// <summary>
        /// Map a synchronous function <paramref name="mapping"/> over the result of an
        /// <see cref="DataTask{TResult}"/> to obtain an <see cref="DataTask{TResult}"/>.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="bound"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public static DataTask<TOut> Select<TIn, TOut>
            (this DataTask<TIn> bound, Func<TIn, TOut> mapping)
            => MapTask<TIn, TOut>.Create(bound, mapping);

        /// <summary>
        /// Chain a dependent task onto an <see cref="DataTask{TResult}"/> to obtain an <see cref="DataTask{TResult}"/>.
        /// <paramref name="continuation"/> uses the result of the <paramref name="bound"/> task to decide
        /// what task to perform next.
        /// </summary>
        /// <typeparam name="TPending"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="bound"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static DataTask<TOut> Bind<TPending, TOut>
            (this DataTask<TPending> bound, Func<TPending, DataTask<TOut>> continuation)
            => BindTask<TPending, TOut>.Create(bound, continuation);

        /// <summary>
        /// Chain a dependent task onto an <see cref="DataTask{TResult}"/> to obtain an <see cref="DataTask{TResult}"/>.
        /// <paramref name="continuation"/> uses the result of the <paramref name="bound"/> task to decide
        /// what task to perform next.
        /// </summary>
        /// <typeparam name="TPending"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="bound"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static DataTask<TOut> BindF<TPending, TOut>
            (this DataTask<TPending> bound, FSharpFunc<TPending, DataTask<TOut>> continuation)
            => BindTask<TPending, TOut>.Create(bound, continuation);

        /// <summary>
        /// Alias for <see cref="Bind{TPending,TOut}(DataTask{TPending},Func{TPending,DataTask{TOut}})"/>
        /// used in LINQ expression syntax.
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
        public static DataTask<TOut> SelectMany<TPending, TOut>
            (this DataTask<TPending> bound, Func<TPending, DataTask<TOut>> continuation)
            => BindTask<TPending, TOut>.Create(bound, continuation);

        /// <summary>
        /// Create a finished <see cref="DataTask{TResult}"/> whose result is <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DataTask<T> Return<T>(T value) => new DataTask<T>(value);

        /// <summary>
        /// Compose two <see cref="DataTask{TResult}"/>s into one, creating its result by by applying
        /// the function returned by the first to the input returned by the second.
        /// </summary>
        /// <remarks>
        /// Because the two <see cref="DataTask{TResult}"/>s are independent, they will run concurrently.
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
        public static DataTask<TOut> Apply<T, TOut>
            (this DataTask<Func<T, TOut>> functionTask, DataTask<T> inputTask)
            => ApplyTask<T, TOut>.Create(functionTask, inputTask);

        /// <summary>
        /// Combine an <see cref="DataTask{TResult}"/> and an <see cref="DataTask{TResult}"/> into
        /// an <see cref="DataTask{TResult}"/> using the function <paramref name="zipper"/>.
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
        public static DataTask<TOut> Zip<TLeft, TRight, TOut>
            (this DataTask<TLeft> left, DataTask<TRight> right, Func<TLeft, TRight, TOut> zipper)
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
        public static DataTask<TSum> Sum<T, TSum>
            (this IEnumerable<DataTask<T>> tasks, TSum initial, Func<TSum, T, TSum> add)
            => SumTask<T, TSum>.Create(tasks, initial, add);

        /// <summary>
        /// Wrap an <see cref="DataTask{TResult}"/> with an exception handler, which defines the task
        /// to fall back to in the event of an exception.
        /// </summary>
        /// <remarks>
        /// This catches exceptions that occur while executing the <see cref="IDataRequest"/>s generated by
        /// the <see cref="DataTask{TResult}"/>, as well as exceptions thrown within calls to
        /// <see cref="DataTask{TResult}.Step"/>.
        /// 
        /// <paramref name="exceptionHandler"/> should re-raise the given exception if it cannot handle it.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        public static DataTask<T> TryCatch<T>
            (this DataTask<T> wrapped, Func<Exception, DataTask<T>> exceptionHandler)
            => TryCatchTask<T>.Create(wrapped, exceptionHandler);

        /// <summary>
        /// Wrap an <see cref="DataTask{TResult}"/> with an exception handler, which defines the task
        /// to fall back to in the event of an exception.
        /// </summary>
        /// <remarks>
        /// This catches exceptions that occur while executing the <see cref="IDataRequest"/>s generated by
        /// the <see cref="DataTask{TResult}"/>, as well as exceptions thrown within calls to
        /// <see cref="DataTask{TResult}.Step"/>, and exceptions thrown in the given <paramref name="wrapped"/> function.
        /// 
        /// <paramref name="exceptionHandler"/> should re-raise the given exception if it cannot handle it.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        public static DataTask<T> TryCatch<T>
            (this Func<DataTask<T>> wrapped, Func<Exception, DataTask<T>> exceptionHandler)
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
        /// Wrap an <see cref="DataTask{TResult}"/> with a completion action, which executes when <paramref name="wrapped"/>
        /// finishes regardless of whether <paramref name="wrapped"/> produced a result, failed with an exception,
        /// or was cancelled due to an exception thrown from a concurrently applied task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="onExit"></param>
        /// <returns></returns>
        public static DataTask<T> TryFinally<T>(this DataTask<T> wrapped, Action onExit)
            => TryFinallyTask<T>.Create(wrapped, onExit);

        /// <summary>
        /// Wrap an <see cref="DataTask{TResult}"/> with a completion action, which executes when <paramref name="wrapped"/>
        /// finishes regardless of whether <paramref name="wrapped"/> produced a result, failed with an exception,
        /// was cancelled due to an exception thrown from a concurrently applied task, or failed to be generated
        /// by the given function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="onExit"></param>
        /// <returns></returns>
        public static DataTask<T> TryFinally<T>(this Func<DataTask<T>> wrapped, Action onExit)
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
        public static DataTask<TVoid> ForEach<TElement, TVoid>
            (this IDataEnumerable<TElement> enumerable, Func<TElement, DataTask<TVoid>> iteration)
            => ForEachDataEnumerableTask<TElement, TVoid>.Create(enumerable, iteration);

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
        public static DataTask<TVoid> ForEach<TElement, TVoid>
            (this IEnumerable<TElement> enumerable, Func<TElement, DataTask<TVoid>> iteration)
            => ForEachEnumerableTask<TElement, TVoid>.Create(enumerable, iteration);

        /// <summary>
        /// Create an <see cref="DataTask{TResult}"/> which asynchronously iterates an <see cref="IDataEnumerable{T}"/>
        /// to produce a materialized list of its elements.
        /// </summary>
        /// <remarks>
        /// This is the typical way to get a normal collection type from an <see cref="IDataEnumerable{T}"/>.
        /// </remarks>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static DataTask<List<TElement>> ToList<TElement>
            (this IDataEnumerable<TElement> enumerable)
        {
            var list = new List<TElement>();
            return ForEachDataEnumerableTask<TElement, bool>.Create
                (enumerable, e => { list.Add(e); return Return(false); }).Select(_ => list);
        }

        public static DataTask<TAccum> Aggregate<TElement, TAccum>
            (this IDataEnumerable<TElement> enumerable, TAccum initial, Func<TAccum, TElement, TAccum> aggregator)
        {
            var current = initial;
            return ForEachDataEnumerableTask<TElement, bool>.Create
                (enumerable, e => { current = aggregator(current, e); return Return(false); }).Select(_ => current);
        }

        /// <summary>
        /// Use a <typeparamref name="TDisposable"/> in a <see cref="DataTask{TResult}"/>, producing a task which will
        /// reliably dispose of the <typeparamref name="TDisposable"/> on completion regardless of exceptions.
        /// </summary>
        /// <typeparam name="TDisposable"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="getDisposable"></param>
        /// <param name="getBlock"></param>
        /// <returns></returns>
        public static DataTask<T> Using<TDisposable, T>
            (this Func<TDisposable> getDisposable, Func<TDisposable, DataTask<T>> getBlock)
            where TDisposable : IDisposable
        {
            var disposable = getDisposable();
            DataTask<T> block;
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
