using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a set of tasks whose results will be combined into a sum in order of completion.
    /// Can be more efficient than folding a deeply nested `ApplicativeTask`.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TSum"></typeparam>
    internal class SumTask<T, TSum> : IDataTask<TSum>
    {
        private readonly IEnumerable<IDataTask<T>> _tasks;
        private readonly TSum _accumulator;
        private readonly Func<TSum, T, TSum> _add;

        public SumTask(IEnumerable<IDataTask<T>> tasks, TSum accumulator, Func<TSum, T, TSum> add)
        {
            _tasks = tasks;
            _accumulator = accumulator;
            _add = add;
        }

        public StepState<TSum> Step()
        {
            var steps = _tasks.Select(t => t.Step());
            var sum = _accumulator;
            var pendings = new List<RequestsPending<T>>();
            foreach (var step in steps)
            {
                step.Visit(pending =>
                {
                    pendings.Add(pending);
                    return default(TSum);
                }, result => sum = _add(sum, result));
            }
            if (pendings.Count <= 0) return StepState.Result(sum);
            var sumPending = new RequestsPending<TSum>
                ( new BatchBranchN<IDataRequest>(pendings.Select(p => p.Requests).ToList())
                , response =>
                {
                    var branchN = response.AssumeBranchN();
                    var nextTasks = branchN.Children.Zip(pendings, (subResponse, subPending) => subPending.Resume(subResponse));
                    return new SumTask<T, TSum>(nextTasks, sum, _add);
                }
                , exceptions =>
                {
                    var branchN = exceptions.AssumeBranchN();
                    var toRethrow = new List<Exception>();
                    var toContinue = new List<IDataTask<T>>();
                    var pairs = branchN.Children.Zip(pendings, (subExceptions, subPending) => new { subExceptions, subPending });
                    var allGood = true;
                    foreach (var pair in pairs)
                    {
                        
                    }
                    return null;
                });
            return StepState.Pending(sumPending);
        }
    }
}
