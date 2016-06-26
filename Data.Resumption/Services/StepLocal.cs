namespace Data.Resumption.Services
{
    /// <summary>
    /// Wraps a <typeparamref name="TService"/>. This can always be resolved by a <see cref="IServiceContext"/>
    /// to get an instance of the <typeparamref name="TService"/> with step-local lifetime.
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    public class StepLocal<TService> where TService : new()
    {
        public TService Service { get; } = new TService();
    }
}
