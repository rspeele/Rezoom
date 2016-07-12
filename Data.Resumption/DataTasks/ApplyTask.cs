using System;
using Microsoft.FSharp.Core;

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
                    var functionNext = default(DataTask<Func<TIn, TOut>>);
                    var inputNext = default(DataTask<TIn>);
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

        private static StepState<TOut> Step(DataTask<Func<TIn, TOut>> functionTask, DataTask<TIn> inputTask)
        {
            Exception functionException = null, inputException = null;
            var functionStep = default(StepState<Func<TIn, TOut>>);
            var inputStep = default(StepState<TIn>);
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

        public static DataTask<TOut> Create(DataTask<Func<TIn, TOut>> functionTask, DataTask<TIn> inputTask)
            => new DataTask<TOut>(() => Step(functionTask, inputTask));

        // Duplicate specialized for FSharpFunc

        private static RequestsPending<TOut> BothPending
            (RequestsPending<FSharpFunc<TIn, TOut>> functionPending, RequestsPending<TIn> inputPending)
            => new RequestsPending<TOut>
            (new BatchBranch2<IDataRequest>(functionPending.Requests, inputPending.Requests)
                , responses =>
                {
                    var branch = responses.AssumeBranch2();
                    Exception functionException = null, inputException = null;
                    var functionNext = default(DataTask<FSharpFunc<TIn, TOut>>);
                    var inputNext = default(DataTask<TIn>);
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

        private static StepState<TOut> Step(DataTask<FSharpFunc<TIn, TOut>> functionTask, DataTask<TIn> inputTask)
        {
            Exception functionException = null, inputException = null;
            var functionStep = default(StepState<FSharpFunc<TIn, TOut>>);
            var inputStep = default(StepState<TIn>);
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
                        ( functionPending.Map(dt => dt.Select(f => f.Invoke(result)))))
                        , function => inputStep.Match
                            ( inputPending => StepState.Pending(inputPending.Map(dt => dt.SelectF(function)))
                            , input => StepState.Result(function.Invoke(input))));
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

        public static DataTask<TOut> Create(DataTask<FSharpFunc<TIn, TOut>> functionTask, DataTask<TIn> inputTask)
            => new DataTask<TOut>(() => Step(functionTask, inputTask));
    }
}
