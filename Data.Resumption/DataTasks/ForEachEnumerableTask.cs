using System;
using System.Collections.Generic;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents iteration over a lazily evaluated sequence.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TVoid"></typeparam>
    internal class ForEachEnumerableTask<TElement, TVoid> : IDataTask<TVoid>
    {
        private readonly IEnumerable<TElement> _enumerable;
        private readonly Func<TElement, IDataTask<TVoid>> _iteration;

        public ForEachEnumerableTask(IEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
        {
            _enumerable = enumerable;
            _iteration = iteration;
        }

        private IDataTask<TVoid> Iterate(IEnumerator<TElement> enumerator)
        {
            if (enumerator.MoveNext()) return DataTask.Return(default(TVoid));
            return _iteration(enumerator.Current)
                .Bind(_ => Iterate(enumerator));
        }

        public StepState<TVoid> Step()
            => DataTask.Using(() => _enumerable.GetEnumerator(), Iterate).Step();
    }
}