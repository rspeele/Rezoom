namespace Data.Resumption
{
    public interface IDataTask<TResult>
    {
        StepState<TResult> Step();
    }
}