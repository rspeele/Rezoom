namespace Data.Resumption.Services
{
    /// <summary>
    /// Pairs a service of type <typeparamref name="T"/> with its lifetime.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct LivingService<T>
    {
        public LivingService(T service, ServiceLifetime lifetime)
        {
            Service = service;
            Lifetime = lifetime;
        }

        public T Service { get; }
        public ServiceLifetime Lifetime { get; }
    }
}