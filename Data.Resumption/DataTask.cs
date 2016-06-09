using System;
using System.Collections.Generic;
using Data.Resumption.DataTasks;

namespace Data.Resumption
{
    public static class DataTask
    {
        public static IDataTask<T> ToDataTask<T>(this IDataRequest<T> dataRequest)
            => new RequestTask<T>(dataRequest);

        public static IDataTask<TOut> Select<TIn, TOut>(this IDataTask<TIn> bound, Func<TIn, TOut> mapping)
            => new MapTask<TIn, TOut>(bound, mapping);

        public static IDataTask<TOut> SelectMany<TPending, TOut>(this IDataTask<TPending> bound, Func<TPending, IDataTask<TOut>> continuation)
            => new BindTask<TPending, TOut>(bound, continuation);

        public static IDataTask<T> Return<T>(T value) => new ReturnTask<T>(value);

        public static IDataTask<TOut> Apply<T, TOut>(this IDataTask<Func<T, TOut>> functionTask, IDataTask<T> inputTask)
            => new ApplyTask<T, TOut>(functionTask, inputTask);

        public static IDataTask<TSum> Sum<T, TSum>(this IEnumerable<IDataTask<T>> tasks, TSum initial, Func<TSum, T, TSum> add)
            => new SumTask<T, TSum>(tasks, initial, add);

        public static IDataTask<T> TryCatch<T>(this IDataTask<T> wrapped, Func<Exception, IDataTask<T>> exceptionHandler)
            => new TryCatchTask<T>(wrapped, exceptionHandler);

        public static IDataTask<T> TryFinally<T>(this IDataTask<T> wrapped, Action onExit)
            => new TryFinallyTask<T>(wrapped, onExit);
    }
}
