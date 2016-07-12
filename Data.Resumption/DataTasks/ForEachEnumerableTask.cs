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
        public static IDataTask<TVoid> Create
            (IEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
            => new IDataTask<TVoid>(() => Step(enumerable, iteration));

        private static IDataTask<TVoid> Iterate
            (IEnumerator<TElement> enumerator, Func<TElement, IDataTask<TVoid>> iteration)
        {
            if (enumerator.MoveNext()) return DataTask.Return(default(TVoid));
            return iteration(enumerator.Current)
                .Bind(_ => Iterate(enumerator, iteration));
        }

        public static StepState<TVoid> Step
            (IEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
            => DataTask.Using(enumerable.GetEnumerator, d => Iterate(d, iteration)).Step();
    }
}