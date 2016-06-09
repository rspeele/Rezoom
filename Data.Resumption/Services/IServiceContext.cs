namespace Data.Resumption.Services
{
    /// <summary>
    /// Provides the current instance for any service type
    /// (e.g. a database connection) within an execution context.
    /// </summary>
    /// <remarks>
    /// This is what data requests are given access to, and use to obtain their connections / etc.
    /// </remarks>
    public interface IServiceContext
    {
        /// <summary>
        /// Get the execution context's instance for the given service, creating a new one
        /// if necessary. Should always return the same instance when called more than once
        /// with the same type parameter.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        TService GetService<TService>();
    }
}
