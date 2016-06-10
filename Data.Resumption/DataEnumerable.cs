using System;
using Data.Resumption.DataEnumerables;

namespace Data.Resumption
{
    public static class DataEnumerable
    {
        public static IDataEnumerable<TOut> SelectMany<TIn, TOut>
            (this IDataEnumerable<TIn> inputs, Func<TIn, IDataEnumerable<TOut>> selector)
            => new BindEnumerable<TIn, TOut>(inputs, selector);

        public static IDataEnumerable<TOut> SelectMany<TIn, TOut>
            (this IDataTask<TIn> input, Func<TIn, IDataEnumerable<TOut>> selector)
            => new BindTaskEnumerable<TIn, TOut>(input, selector);

        public static IDataEnumerable<T> Combine<T>
            (this IDataEnumerable<T> first, Func<IDataEnumerable<T>> second)
            => new CombineEnumerable<T>(first, second);
    }
}