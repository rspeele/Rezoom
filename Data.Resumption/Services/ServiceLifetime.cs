namespace Data.Resumption.Services
{
    /// <summary>
    /// Denotes how long a service obtained from a service factory should be retained.
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// The service can be retained indefinitely, across execution contexts.
        /// The service will never be garbage collected or disposed.
        /// </summary>
        /// <remarks>
        /// This may be suitable for static, stateless singletons.
        /// </remarks>
        Eternal = 1,
        /// <summary>
        /// Each execution context should request its own instance of the service from the service factory.
        /// The service will be disposed with the execution context.
        /// </summary>
        /// <remarks>
        /// This is the most reasonable choice for most services.
        /// </remarks>
        ExecutionContext = 2,
        /// <summary>
        /// Each pending execution step should request its own instance of the service from the service factory.
        /// The service will be disposed after execution of the step's data requests.
        /// </summary>
        ExecutionStep = 3,
    }
}