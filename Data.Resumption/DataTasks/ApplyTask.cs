using System;

namespace Data.Resumption.DataTasks
{
    /// <summary>
    /// Implements a chain of tasks where the first task produces a function
    /// to transform the output of the second task. This allows the two tasks
    /// to execute "interleaved", since they are not dependent on each other until
    /// the end.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    internal static class ApplyTask<TIn, TOut>
    {
        private static RequestsPending<TOut> BothPending
            (RequestsPending<Func<TIn, TOut>> functionPending, RequestsPending<TIn> inputPending)
            => new RequestsPending<TOut>
            (new BatchBranch2<IDataRequest>(functionPending.Requests, inputPending.Requests)
                , responses =>
                {
                    var branch = responses.AssumeBranch2();
                    Exception functionException = null, inputException = null;
                    var functionNext = default(IDataTask<Func<TIn, TOut>>);
                    var inputNext = default(IDataTask<TIn>);
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
                        return Create(functionNext, inputNext);
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
                        inputNext.Abort(functionException);
                        throw functionException;
                    }
                    functionNext.Abort(inputException);
                    throw inputException;
                });

        private static StepState<TOut> Step(IDataTask<Func<TIn, TOut>> functionTask, IDataTask<TIn> inputTask)
        {
            Exception functionException = null, inputException = null;
            StepState<Func<TIn, TOut>> functionStep = null;
            StepState<TIn> inputStep = null;
            try { functionStep = functionTask.Step(); }
            catch (Exception ex) { functionException = ex; }
            try { inputStep = inputTask.Step(); }
            catch (Exception ex) { inputException = ex; }
            if (functionException == null && inputException == null)
            {
                return functionStep.Match
                    ( functionPending => inputStep.Match(inputPending =>
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
            if (functionException != null && inputException != null)
            {
                throw new AggregateException(functionException, inputException);
            }
            if (functionException != null)
            {
                inputStep.Abort(functionException);
                throw functionException;
            }
            functionStep.Abort(inputException);
            throw inputException;
        }

        public static IDataTask<TOut> Create(IDataTask<Func<TIn, TOut>> functionTask, IDataTask<TIn> inputTask)
            => new IDataTask<TOut>(() => Step(functionTask, inputTask));
    }
}
