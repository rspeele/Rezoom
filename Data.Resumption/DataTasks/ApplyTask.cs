using System;
using System.Runtime.ExceptionServices;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Represents a chain of tasks where the first task produces a function
    /// to transform the output of the second task. This allows the two tasks
    /// to execute "interleaved", since they are not dependent on each other until
    /// the end.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    public class ApplyTask<TIn, TOut> : IDataTask<TOut>
    {
        private readonly IDataTask<Func<TIn, TOut>> _functionTask;
        private readonly IDataTask<TIn> _inputTask;

        public ApplyTask(IDataTask<Func<TIn, TOut>> functionTask, IDataTask<TIn> inputTask)
        {
            _functionTask = functionTask;
            _inputTask = inputTask;
        }

        private static RequestsPending<TOut> BothPending(RequestsPending<Func<TIn, TOut>> functionPending, RequestsPending<TIn> inputPending)
            => new RequestsPending<TOut>
            (new BatchBranch2<IDataRequest>(functionPending.Requests, inputPending.Requests)
                , responses =>
                {
                    var branch = responses.AssumeBranch2();
                    Exception functionException = null, inputException = null;
                    IDataTask<Func<TIn, TOut>> functionNext = null;
                    IDataTask<TIn> inputNext = null;
                    try
                    {
                        functionNext = functionPending.Resume(branch.Left);
                    }
                    catch (Exception ex)
                    {
                        functionException = ex;
                    }
                    try
                    {
                        inputNext = inputPending.Resume(branch.Right);
                    }
                    catch (Exception ex)
                    {
                        inputException = ex;
                    }
                    if (functionException == null && inputException == null)
                    {
                        return new ApplyTask<TIn, TOut>(functionNext, inputNext);
                    }
                    // We got exceptions in one or both tasks, which they failed to recover from.
                    // If they both failed, they'll have already run their `finally` code. We can safely bail.
                    if (functionException != null && inputException != null)
                    {
                        throw new AggregateException(functionException, inputException);
                    }
                    // If only one failed, the other needs to get an exception so it can recover.
                    if (functionException != null)
                    {
                        try
                        {
                            inputNext?.Step().Match(pending => pending.Resume(new BatchAbortion<SuccessOrException>()), _ => null);
                        }
                        catch (Exception nextEx)
                        {
                            throw new AggregateException(functionException, nextEx);
                        }
                        ExceptionDispatchInfo.Capture(functionException).Throw();
                        throw functionException; // should be impossible to get here
                    }
                    // ReSharper disable once RedundantIfElseBlock it looks better this way
                    else
                    {
                        try
                        {
                            functionNext?.Step().Match(pending => pending.Resume(new BatchAbortion<SuccessOrException>()), _ => null);
                        }
                        catch (Exception nextEx)
                        {
                            throw new AggregateException(inputException, nextEx);
                        }
                        ExceptionDispatchInfo.Capture(inputException).Throw();
                        throw inputException; // should be impossible to get here
                    }
                });

        public StepState<TOut> Step()
        {
            var functionStep = _functionTask.Step();
            var inputStep = _inputTask.Step();
            return functionStep.Match
                (functionPending => inputStep.Match(inputPending =>
                    {
                        var bothPending = BothPending(functionPending, inputPending);
                        return StepState.Pending(bothPending);
                    }
                , result => StepState.Pending
                    ( functionPending.Map(dt => dt.Select(f => f(result)))))
                    , function => inputStep.Match
                        ( inputPending => StepState.Pending(inputPending.Map(dt => dt.Select(function)))
                        , input => StepState.Result(function(input))));
        }
    }
}
