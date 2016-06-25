namespace Data.Resumption
{
    /// <summary>
    /// Represents an asynchronous computation which will eventually produce a <typeparamref name="TResult"/>.
    /// 
    /// It can be stepped to obtain either:
    ///   a. the result
    /// or
    ///   b. a pending data command and a continuation function
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public interface IDataTask<TResult>
    {
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
        StepState<TResult> Step();
    }
}