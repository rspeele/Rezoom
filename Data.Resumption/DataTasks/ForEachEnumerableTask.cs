using System;
using System.Collections.Generic;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Implements iteration over a lazily evaluated sequence.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TVoid"></typeparam>
    internal static class ForEachEnumerableTask<TElement, TVoid>
    {
        public static DataTask<TVoid> Create
            (IEnumerable<TElement> enumerable, Func<TElement, DataTask<TVoid>> iteration)
            => new DataTask<TVoid>(() => Step(enumerable, iteration));

        private static DataTask<TVoid> Iterate
            (IEnumerator<TElement> enumerator, Func<TElement, DataTask<TVoid>> iteration)
        {
            if (enumerator.MoveNext()) return DataTask.Return(default(TVoid));
            return iteration(enumerator.Current)
                .Bind(_ => Iterate(enumerator, iteration));
        }

        public static StepState<TVoid> Step
            (IEnumerable<TElement> enumerable, Func<TElement, DataTask<TVoid>> iteration)
            => DataTask.Using(enumerable.GetEnumerator, d => Iterate(d, iteration)).Step();
    }
}