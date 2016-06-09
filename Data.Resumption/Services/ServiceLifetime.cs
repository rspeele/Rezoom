namespace Data.Resumption.Services
{
    /// <summary>
    /// Denotes how long a service obtained from a service factory should be retained.
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// Each execution context should request its own instance of the service from the service factory.
        /// The service will be disposed with the execution context.
        /// </summary>
        /// <remarks>
        /// This is the most reasonable choice for most services.
        /// </remarks>
        ExecutionContext = 1,
        /// <summary>
        /// Each pending execution step should request its own instance of the service from the service factory.
        /// The service will be disposed after execution of the step's data requests.
        /// </summary>
        ExecutionStep = 2,
    }
}