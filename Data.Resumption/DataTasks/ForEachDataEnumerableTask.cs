using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents iteration over a lazily evaluated asynchronous sequence.
    /// Always returns default(<typeparamref name="TVoid"/>).
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TVoid"></typeparam>
    internal class ForEachDataEnumerableTask<TElement, TVoid> : IDataTask<TVoid>
    {
        private readonly IDataEnumerable<TElement> _enumerable;
        private readonly Func<TElement, IDataTask<TVoid>> _iteration;

        public ForEachDataEnumerableTask(IDataEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
        {
            _enumerable = enumerable;
            _iteration = iteration;
        }

        private IDataTask<TVoid> Iterate(IDataEnumerator<TElement> enumerator)
            => enumerator.MoveNext().Bind(yield =>
                yield.HasValue
                ? _iteration(yield.Value).Bind(_ => Iterate(enumerator))
                : DataTask.Return(default(TVoid)));

        public StepState<TVoid> Step() 
            => DataTask.Using(() => _enumerable.GetEnumerator(), Iterate).Step();
    }
}
