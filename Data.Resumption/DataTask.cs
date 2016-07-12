using System;

namespace Data.Resumption
{
    public enum DataTaskTag : byte
    {
        StepState,
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
    public struct DataTask<TResult>
    {
        private readonly DataTaskTag _tag;
        private readonly StepState<TResult> _stepState;
        private readonly Func<StepState<TResult>> _csStep;

        internal DataTask(StepState<TResult> stepState)
        {
            _tag = DataTaskTag.StepState;
            _csStep = null;
            _stepState = stepState;
        }

        internal DataTask(TResult result) : this(StepState.Result(result)) { }
        internal DataTask(RequestsPending<TResult> pending) : this (StepState.Pending(pending)) { }
        internal DataTask(Func<StepState<TResult>> csStep)
        {
            _tag = DataTaskTag.CsStep;
            _stepState = default(StepState<TResult>);
            _csStep = csStep;
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
        /// <see cref="DataTask{TResult}"/>. Rather, to proceed with execution when pending requests have been fulfilled,
        /// callers should use the <see cref="RequestsPending{TResult}.Resume"/> method.
        /// </remarks>
        /// <returns></returns>
        internal static StepState<TResult> InternalStep(DataTask<TResult> task)
        {
            switch (task._tag)
            {
                case DataTaskTag.StepState: return task._stepState;
                case DataTaskTag.CsStep: return task._csStep();
                default:
                    throw new ArgumentOutOfRangeException(nameof(_tag));
            }
        }
    }
}