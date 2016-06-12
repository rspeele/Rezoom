using System;
using System.Collections.Generic;
using Data.Resumption.DataEnumerables;

namespace Data.Resumption
{
    public static class DataEnumerable
    {
        public static IDataEnumerable<T> Zero<T>()
            => new ZeroEnumerable<T>();

        public static IDataEnumerable<T> Yield<T>(T value)
            => new YieldEnumerable<T>(value);

        public static IDataEnumerable<T> YieldMany<T>(IEnumerable<T> values)
            => new YieldManyEnumerable<T>(values);

        public static IDataEnumerable<TOut> SelectMany<TIn, TOut>
            (this IDataEnumerable<TIn> inputs, Func<TIn, IDataEnumerable<TOut>> selector)
            => new BindEnumerable<TIn, TOut>(inputs, selector);

        public static IDataEnumerable<TOut> SelectMany<TIn, TOut>
            (this IDataTask<TIn> input, Func<TIn, IDataEnumerable<TOut>> selector)
            => new BindTaskEnumerable<TIn, TOut>(input, selector);

        public static IDataEnumerable<TOut> Select<TIn, TOut>
            (this IDataEnumerable<TIn> inputs, Func<TIn, TOut> mapping)
            => new MapEnumerable<TIn, TOut>(inputs, mapping);

        public static IDataEnumerable<T> Where<T>
            (this IDataEnumerable<T> inputs, Func<T, bool> predicate)
            => new FilterEnumerable<T>(inputs, predicate);

        public static IDataEnumerable<T> Where<T>
            (this IDataEnumerable<T> inputs, Func<T, IDataTask<bool>> predicate)
            => inputs.SelectMany(e => predicate(e).SelectMany(t => t ? Yield(e) : Zero<T>()));

        public static IDataEnumerable<T> Take<T>
            (this IDataEnumerable<T> inputs, int count)
            => new TakeEnumerable<T>(inputs, count);

        public static IDataEnumerable<T> TakeWhile<T>
            (this IDataEnumerable<T> inputs, Func<T, IDataTask<bool>> predicate)
            => new TakeWhileEnumerable<T>(inputs, predicate);

        public static IDataEnumerable<T> TakeWhile<T>
            (this IDataEnumerable<T> inputs, Func<T, bool> predicate)
            => inputs.TakeWhile(x => DataTask.Return(predicate(x)));

        public static IDataEnumerable<TOut> Zip<TLeft, TRight, TOut>
            ( IDataEnumerable<TLeft> left
            , IDataEnumerable<TRight> right
            , Func<TLeft, TRight, IDataTask<TOut>> zipper
            ) => new ZipEnumerable<TLeft, TRight, TOut>(left, right, zipper);

        public static IDataEnumerable<TOut> Zip<TLeft, TRight, TOut>
            ( IDataEnumerable<TLeft> left
            , IDataEnumerable<TRight> right
            , Func<TLeft, TRight, TOut> zipper
            ) => new ZipEnumerable<TLeft, TRight, TOut>
                (left, right, (l, r) => DataTask.Return(zipper(l, r)));

        public static IDataEnumerable<T> Combine<T>
            (this IDataEnumerable<T> first, Func<IDataEnumerable<T>> second)
            => new CombineEnumerable<T>(first, second);

        public static IDataEnumerable<T> TryCatch<T>
            (this IDataEnumerable<T> wrapped, Func<Exception, IDataEnumerable<T>> exceptionHandler)
            => new TryCatchEnumerable<T>(wrapped, exceptionHandler);

        public static IDataEnumerable<T> TryCatch<T>
            (this Func<IDataEnumerable<T>> wrapped, Func<Exception, IDataEnumerable<T>> exceptionHandler)
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

        public static IDataEnumerable<T> TryFinally<T>
            (this IDataEnumerable<T> wrapped, Action onExit)
            => new TryFinallyEnumerable<T>(wrapped, onExit);

        public static IDataEnumerable<T> TryFinally<T>
            (this Func<IDataEnumerable<T>> wrapped, Action onExit)
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
    }
}