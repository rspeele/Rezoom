namespace Data.Resumption.Services
{
    /// <summary>
    /// Provides services and specifies their lifetime.
    /// </summary>
    public interface IServiceFactory
    {
        /// <summary>
        /// Attempt to create a new service of the given time, with its lifetime specified.
        /// Return null if the service type is not supported.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        LivingService<T>? CreateService<T>();
    }
}
