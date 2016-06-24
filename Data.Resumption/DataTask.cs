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

        public static IDataTask<T> Return<T>(T value) => new ReturnTask<T>(value);

        public static IDataTask<TOut> Apply<T, TOut>
            (this IDataTask<Func<T, TOut>> functionTask, IDataTask<T> inputTask)
            => new ApplyTask<T, TOut>(functionTask, inputTask);

        public static IDataTask<TOut> Zip<TLeft, TRight, TOut>
            (this IDataTask<TLeft> left, IDataTask<TRight> right, Func<TLeft, TRight, TOut> zipper)
            => left
                .Select(lf => (Func<TRight, TOut>)(rt => zipper(lf, rt)))
                .Apply(right);

        public static IDataTask<TSum> Sum<T, TSum>
            (this IEnumerable<IDataTask<T>> tasks, TSum initial, Func<TSum, T, TSum> add)
            => new SumTask<T, TSum>(tasks, initial, add);

        public static IDataTask<T> TryCatch<T>
            (this IDataTask<T> wrapped, Func<Exception, IDataTask<T>> exceptionHandler)
            => new TryCatchTask<T>(wrapped, exceptionHandler);

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

        public static IDataTask<T> TryFinally<T>(this IDataTask<T> wrapped, Action onExit)
            => new TryFinallyTask<T>(wrapped, onExit);

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

        public static IDataTask<TVoid> ForEach<TElement, TVoid>
            (this IDataEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
            => new ForEachDataEnumerableTask<TElement, TVoid>(enumerable, iteration);

        public static IDataTask<TVoid> ForEach<TElement, TVoid>
            (this IEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
            => new ForEachEnumerableTask<TElement, TVoid>(enumerable, iteration);

        public static IDataTask<List<TElement>> ToList<TElement>
            (this IDataEnumerable<TElement> enumerable)
        {
            var list = new List<TElement>();
            return new ForEachDataEnumerableTask<TElement, bool>
                (enumerable, e => { list.Add(e); return Return(false); }).Select(_ => list);
        }

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
