using System;
using System.Collections.Generic;
using Data.Resumption.DataEnumerables;

namespace Data.Resumption
{
    public static class DataEnumerable
    {
        /// <summary>
        /// Create an empty <see cref="IDataEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IDataEnumerable<T> Zero<T>()
            => new ZeroEnumerable<T>();

        /// <summary>
        /// Create an <see cref="IDataEnumerable{T}"/> whose single element is <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IDataEnumerable<T> Yield<T>(T value)
            => new YieldEnumerable<T>(value);

        /// <summary>
        /// Create an <see cref="IDataEnumerable{T}"/> whose elements are <paramref name="values"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IDataEnumerable<T> YieldMany<T>(IEnumerable<T> values)
            => new YieldManyEnumerable<T>(values);

        /// <summary>
        /// Concatenate subsequences generated from each element of an <see cref="IDataEnumerable{TIn}"/>
        /// into an <see cref="IDataEnumerable{TOut}"/>.
        /// </summary>
        /// <remarks>
        /// This is conceptually the same as LINQ's <c>SelectMany</c> on regular <see cref="IEnumerable{T}"/>s.
        /// </remarks>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="inputs"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IDataEnumerable<TOut> SelectMany<TIn, TOut>
            (this IDataEnumerable<TIn> inputs, Func<TIn, IDataEnumerable<TOut>> selector)
            => new BindEnumerable<TIn, TOut>(inputs, selector);

        /// <summary>
        /// Concatenate subsequences generated from each element of an <see cref="IDataEnumerable{TIn}"/>
        /// into an <see cref="IDataEnumerable{TOut}"/>.
        /// </summary>
        /// <remarks>
        /// This is conceptually the same as LINQ's <c>SelectMany</c> on regular <see cref="IEnumerable{T}"/>s.
        /// </remarks>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="inputs"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IDataEnumerable<TOut> SelectMany<TIn, TOut>
            (this IDataEnumerable<TIn> inputs, Func<TIn, IEnumerable<TOut>> selector)
            => new BindEnumerable<TIn, TOut>(inputs, e => YieldMany(selector(e)));

        /// <summary>
        /// Produce an <see cref="IDataEnumerable{TOut}"/> that depends on the execution of an
        /// <see cref="DataTask{TResult}"/>.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="input"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IDataEnumerable<TOut> SelectMany<TIn, TOut>
            (this DataTask<TIn> input, Func<TIn, IDataEnumerable<TOut>> selector)
            => new BindTaskEnumerable<TIn, TOut>(input, selector);

        /// <summary>
        /// Map a synchronous <see cref="Func{TIn,TOut}"/> over the elements of <paramref name="inputs"/>
        /// to get an <see cref="IDataEnumerable{TOut}"/>.
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="inputs"></param>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public static IDataEnumerable<TOut> Select<TIn, TOut>
            (this IDataEnumerable<TIn> inputs, Func<TIn, TOut> mapping)
            => new MapEnumerable<TIn, TOut>(inputs, mapping);

        /// <summary>
        /// Create a new <see cref="IDataEnumerable{T}"/> which filters out elements of <paramref name="inputs"/>
        /// that do not satisfy the synchronous <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputs"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IDataEnumerable<T> Where<T>
            (this IDataEnumerable<T> inputs, Func<T, bool> predicate)
            => new FilterEnumerable<T>(inputs, predicate);

        /// <summary>
        /// Create a new <see cref="IDataEnumerable{T}"/> which filters out elements of <paramref name="inputs"/>
        /// that do not satisfy the asynchronous <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputs"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IDataEnumerable<T> Where<T>
            (this IDataEnumerable<T> inputs, Func<T, DataTask<bool>> predicate)
            => inputs.SelectMany(e => predicate(e).SelectMany(t => t ? Yield(e) : Zero<T>()));

        /// <summary>
        /// Create an <see cref="IDataEnumerable{T}"/> which yields only the first <paramref name="count"/>
        /// elements from <paramref name="inputs"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputs"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IDataEnumerable<T> Take<T>
            (this IDataEnumerable<T> inputs, int count)
            => new TakeEnumerable<T>(inputs, count);

        /// <summary>
        /// Create an <see cref="IDataEnumerable{T}"/> which yields only the initial sub-sequence of elements
        /// from <paramref name="inputs"/> that satisfy the asynchronous <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputs"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IDataEnumerable<T> TakeWhile<T>
            (this IDataEnumerable<T> inputs, Func<T, DataTask<bool>> predicate)
            => new TakeWhileEnumerable<T>(inputs, predicate);

        /// <summary>
        /// Create an <see cref="IDataEnumerable{T}"/> which yields only the initial sub-sequence of elements
        /// from <paramref name="inputs"/> that satisfy the synchronous <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputs"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IDataEnumerable<T> TakeWhile<T>
            (this IDataEnumerable<T> inputs, Func<T, bool> predicate)
            => inputs.TakeWhile(x => DataTask.Return(predicate(x)));

        /// <summary>
        /// Apply <paramref name="zipper"/> to the corresponding elements of <paramref name="left"/>
        /// and <paramref name="right"/>, producing a sequence of the results.
        /// </summary>
        /// <remarks>
        /// If the two sequences are of different lengths, the additional elements in the longer sequence will be
        /// ignored. In other words, the resulting sequence will always be as long as the shorter of the two input
        /// sequences.
        /// </remarks>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="zipper"></param>
        /// <returns></returns>
        public static IDataEnumerable<TOut> Zip<TLeft, TRight, TOut>
            ( IDataEnumerable<TLeft> left
            , IDataEnumerable<TRight> right
            , Func<TLeft, TRight, DataTask<TOut>> zipper
            ) => new ZipEnumerable<TLeft, TRight, TOut>(left, right, zipper);

        /// <summary>
        /// Apply <paramref name="zipper"/> to the corresponding elements of <paramref name="left"/>
        /// and <paramref name="right"/>, producing a sequence of the results.
        /// </summary>
        /// <remarks>
        /// If the two sequences are of different lengths, the additional elements in the longer sequence will be
        /// ignored. In other words, the resulting sequence will always be as long as the shorter of the two input
        /// sequences.
        /// </remarks>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="zipper"></param>
        /// <returns></returns>
        public static IDataEnumerable<TOut> Zip<TLeft, TRight, TOut>
            ( IDataEnumerable<TLeft> left
            , IDataEnumerable<TRight> right
            , Func<TLeft, TRight, TOut> zipper
            ) => new ZipEnumerable<TLeft, TRight, TOut>
                (left, right, (l, r) => DataTask.Return(zipper(l, r)));

        /// <summary>
        /// Create an <see cref="IDataEnumerable{T}"/> which yields the elements of <paramref name="first"/>,
        /// then calls <paramref name="second"/> and yields the resulting elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static IDataEnumerable<T> Combine<T>
            (this IDataEnumerable<T> first, Func<IDataEnumerable<T>> second)
            => new CombineEnumerable<T>(first, second);

        /// <summary>
        /// Wrap an <see cref="IDataEnumerable{T}"/> with an exception handler.
        /// If an exception is thrown during iteration of <paramref name="wrapped"/>,
        /// <paramref name="exceptionHandler"/> will be called to produce the remaining elements of
        /// the sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        public static IDataEnumerable<T> TryCatch<T>
            (this IDataEnumerable<T> wrapped, Func<Exception, IDataEnumerable<T>> exceptionHandler)
            => new TryCatchEnumerable<T>(wrapped, exceptionHandler);

        /// <summary>
        /// Wrap an <see cref="IDataEnumerable{T}"/> with an exception handler.
        /// If an exception is thrown during invocation of <paramref name="wrapped"/> or during
        /// iteration of the resulting sequence, <paramref name="exceptionHandler"/> will be called to produce
        /// the remaining elements of the sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Wrap an <see cref="IDataEnumerable{T}"/> with a completion action.
        /// The action will be run whenever an <see cref="IDataEnumerator{T}"/> obtained from
        /// <paramref name="wrapped"/> is disposed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="onExit"></param>
        /// <returns></returns>
        public static IDataEnumerable<T> TryFinally<T>
            (this IDataEnumerable<T> wrapped, Action onExit)
            => new TryFinallyEnumerable<T>(wrapped, onExit);

        /// <summary>
        /// Wrap an <see cref="IDataEnumerable{T}"/> with a completion action.
        /// The action will be run whenever an <see cref="IDataEnumerator{T}"/> obtained from
        /// <paramref name="wrapped"/> is disposed, or if <paramref name="wrapped"/> fails on invocation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wrapped"></param>
        /// <param name="onExit"></param>
        /// <returns></returns>
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