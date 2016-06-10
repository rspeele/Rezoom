using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents iteration over an asynchronous sequence.
    /// Always returns default(<typeparamref name="TVoid"/>).
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TVoid"></typeparam>
    internal class ForEachTask<TElement, TVoid> : IDataTask<TVoid>
    {
        private readonly IDataEnumerable<TElement> _enumerable;
        private readonly Func<TElement, IDataTask<TVoid>> _iteration;

        public ForEachTask(IDataEnumerable<TElement> enumerable, Func<TElement, IDataTask<TVoid>> iteration)
        {
            _enumerable = enumerable;
            _iteration = iteration;
        }

        public StepState<TVoid> Step()
            => _enumerable.Yield()
                .SelectMany(yielded =>
                {
                    if (yielded == null)
                    {
                        return DataTask.Return(default(TVoid));
                    }
                    return _iteration(yielded.Value.Value)
                        .SelectMany(_ =>
                            new ForEachTask<TElement, TVoid>(yielded.Value.Remaining, _iteration));
                }).Step();
    }
}
