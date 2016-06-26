using System;
using System.Collections.Generic;

namespace Data.Resumption.Services.Factories
{
    /// <summary>
    /// A service factory that delegates to a sequence of sub-factories to provide service types.
    /// The first factory in the sequence that supports a type will be used.
    /// </summary>
    public class AggregateServiceFactory : IServiceFactory
    {
        private readonly List<IServiceFactory> _factories = new List<IServiceFactory>();
        /// <summary>
        /// Cached knowledge of which of our factories we use to support each type.
        /// </summary>
        private readonly Dictionary<Type, IServiceFactory> _cache = new Dictionary<Type, IServiceFactory>();

        public AggregateServiceFactory(IEnumerable<IServiceFactory> factories)
        {
            _factories.AddRange(factories);
        }
        public AggregateServiceFactory(params IServiceFactory[] factories)
            : this((IEnumerable<IServiceFactory>)factories) { }

        public LivingService<T>? CreateService<T>(IServiceContext context)
        {
            var ty = typeof(T);
            IServiceFactory cached;
            if (_cache.TryGetValue(ty, out cached)) return cached.CreateService<T>(context);
            foreach (var factory in _factories)
            {
                var living = factory.CreateService<T>(context);
                if (living == null) continue;
                _cache[ty] = factory;
                return living;
            }
            return null;
        }
    }
}