namespace Data.Resumption.Services.Factories
{
    /// <summary>
    /// A service factory that does not support any service types.
    /// </summary>
    public class ZeroServiceFactory : IServiceFactory
    {
        public LivingService<T>? CreateService<T>(IServiceContext context) => null;
    }
}
