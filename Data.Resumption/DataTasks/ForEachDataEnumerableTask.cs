using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents iteration over a lazily evaluated asynchronous sequence.
    /// Always returns default(<typeparamref name="TVoid"/>).
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TVoid"></typeparam>
    internal static class ForEachDataEnumerableTask<TElement, TVoid>
    {
        public static IDataTask<TVoid> Create
            (IDataEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
            => new IDataTask<TVoid>(() => Step(enumerable, iteration));

        private static IDataTask<TVoid> Iterate
            (IDataEnumerator<TElement> enumerator, Func<TElement, IDataTask<TVoid>> iteration)
            => enumerator.MoveNext().Bind(yield =>
                yield.HasValue
                ? iteration(yield.Value).Bind(_ => Iterate(enumerator, iteration))
                : DataTask.Return(default(TVoid)));

        public static StepState<TVoid> Step
            (IDataEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
            => DataTask.Using(enumerable.GetEnumerator, d => Iterate(d, iteration)).Step();
    }
}
