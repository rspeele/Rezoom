using Microsoft.FSharp.Core;
using System;
using System.Runtime.InteropServices;

namespace Data.Resumption
{
    public enum DataTaskTag : uint
    {
        StepState,
        FsStep,
        CsStep,
    }
    /// <summary>
    /// Represents an asynchronous computation which will eventually produce a <typeparamref name="TResult"/>.
    /// 
    /// It can be stepped to obtain either:
    ///   a. the result
    /// or
    ///   b. a pending data command and a continuation function
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public struct IDataTask<TResult>
    {
        private readonly DataTaskTag _tag;
        private readonly StepState<TResult> _stepState;
        private readonly FSharpFunc<Unit, StepState<TResult>> _fsStep;
        private readonly Func<StepState<TResult>> _csStep;

        internal IDataTask(StepState<TResult> stepState)
        {
            _tag = DataTaskTag.StepState;
            _fsStep = null;
            _csStep = null;
            _stepState = stepState;
        }

        internal IDataTask(TResult result) : this(StepState.Result(result)) { }
        internal IDataTask(RequestsPending<TResult> pending) : this (StepState.Pending(pending)) { }
        internal IDataTask(Func<StepState<TResult>> csStep)
        {
            _tag = DataTaskTag.CsStep;
            _stepState = default(StepState<TResult>);
            _fsStep = null;
            _csStep = csStep;
        }
        internal IDataTask(FSharpFunc<Unit, StepState<TResult>> fsStep)
        {
            _tag = DataTaskTag.FsStep;
            _stepState = default(StepState<TResult>);
            _csStep = null;
            _fsStep = fsStep;
        }

        /// <summary>
        /// Get the next <see cref="StepState{T}"/> of this task.
        /// This may be a set of pending <see cref="IDataRequest"/>s or it may be the end result of the task.
        /// </summary>
        /// <remarks>
        /// This is not an inherently stateful operation, although depending on the code running in the task,
        /// it may alter some shared state.
        /// 
        /// It does not typically make sense to call <see cref="Step"/> more than once on the same instance of an
        /// <see cref="IDataTask{T}"/>. Rather, to proceed with execution when pending requests have been fulfilled,
        /// callers should use the <see cref="RequestsPending{TResult}.Resume"/> method.
        /// </remarks>
        /// <returns></returns>
        internal static StepState<TResult> InternalStep(IDataTask<TResult> task)
        {
            switch (task._tag)
            {
                case DataTaskTag.StepState: return task._stepState;
                case DataTaskTag.CsStep: return task._csStep();
                case DataTaskTag.FsStep: return task._fsStep.Invoke(null);
                default:
                    throw new ArgumentOutOfRangeException(nameof(_tag));
            }
        }
    }
}