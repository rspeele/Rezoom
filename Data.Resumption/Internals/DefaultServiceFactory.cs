using System;
using Data.Resumption.Services;

namespace Data.Resumption
{
    /// <summary>
    /// The built-in service factory, which supports creating <see cref="StepLocal{TService}"/>s
    /// and <see cref="ExecutionLocal{TService}"/>s.
    /// </summary>
    internal class DefaultServiceFactory : IServiceFactory
    {
        public LivingService<T>? CreateService<T>(IServiceContext context)
        {
            var type = typeof(T);
            if (!type.IsConstructedGenericType) return null;
            var typeDef = type.GetGenericTypeDefinition();
            var stepLocal = typeDef == typeof(StepLocal<>);
            var executionLocal = typeDef == typeof(ExecutionLocal<>);
            if (!stepLocal && !executionLocal) return null;
            var instance = Activator.CreateInstance(type);
            return new LivingService<T>
                ( (T)instance
                , stepLocal
                    ? ServiceLifetime.ExecutionStep
                    : ServiceLifetime.ExecutionContext
                );
        }
    }
}
