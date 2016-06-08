using System;

namespace Data.Resumption.DataTasks
{
    public static class Extensions
    {
        public static IDataTask<TOut> Select<TIn, TOut>(this IDataTask<TIn> bound, Func<TIn, TOut> mapping)
            => new MapTask<TIn, TOut>(bound, mapping);
        public static IDataTask<TOut> SelectMany<TPending, TOut>(this IDataTask<TPending> bound, Func<TPending, IDataTask<TOut>> continuation)
            => new BindTask<TPending, TOut>(bound, continuation);
    }
}
