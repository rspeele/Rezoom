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
        StepState<TResult> Step();
    }
}