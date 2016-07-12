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
        public static DataTask<TVoid> Create
            (IDataEnumerable<TElement> enumerable, Func<TElement, DataTask<TVoid>> iteration)
            => new DataTask<TVoid>(() => Step(enumerable, iteration));

        private static DataTask<TVoid> Iterate
            (IDataEnumerator<TElement> enumerator, Func<TElement, DataTask<TVoid>> iteration)
            => enumerator.MoveNext().Bind(yield =>
                yield.HasValue
                ? iteration(yield.Value).Bind(_ => Iterate(enumerator, iteration))
                : DataTask.Return(default(TVoid)));

        public static StepState<TVoid> Step
            (IDataEnumerable<TElement> enumerable, Func<TElement, DataTask<TVoid>> iteration)
            => DataTask.Using(enumerable.GetEnumerator, d => Iterate(d, iteration)).Step();
    }
}
