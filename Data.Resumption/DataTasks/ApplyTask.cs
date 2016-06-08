using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a chain of tasks where the first task produces a function
    /// to transform the output of the second task. This allows the two tasks
    /// to execute "interleaved", since they are not dependent on each other until
    /// the end.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    public class ApplyTask<T, TOut> : IDataTask<TOut>
    {
        private readonly IDataTask<Func<T, TOut>> _functionTask;
        private readonly IDataTask<T> _inputTask;

        public ApplyTask(IDataTask<Func<T, TOut>> functionTask, IDataTask<T> inputTask)
        {
            _functionTask = functionTask;
            _inputTask = inputTask;
        }

        public StepState<TOut> Step()
        {
            var functionStep = _functionTask.Step();
            var inputStep = _inputTask.Step();
            return functionStep.Visit
                (functionPending => inputStep.Visit(inputPending =>
                    {
                        var bothPending = new RequestsPending<TOut>
                            ( new BatchBranch2<IDataRequest>(functionPending.Requests, inputPending.Requests)
                            , responses =>
                            {
                                var branch = responses.AssumeBranch2();
                                var functionNext = functionPending.Resume(branch.Left);
                                var inputNext = inputPending.Resume(branch.Right);
                                return new ApplyTask<T, TOut>(functionNext, inputNext);
                            });
                        return StepState.Pending(bothPending);
                    }
                , result => StepState.Pending
                    ( functionPending.Map(dt => dt.Select(f => f(result)))))
                    , function => inputStep.Visit
                        ( inputPending => StepState.Pending(inputPending.Map(dt => dt.Select(function)))
                        , input => StepState.Result(function(input))));
        }
    }
}
