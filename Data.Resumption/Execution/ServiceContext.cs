using System;
using System.Collections.Generic;
using Data.Resumption.Services;

namespace Data.Resumption.Execution
{
    internal class ServiceContext : IServiceContext, IDisposable
    {
        private class Cache : IDisposable
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
            private readonly Stack<IDisposable> _disposalStack = new Stack<IDisposable>();

            public bool TryGetService(Type ty, out object service) => _services.TryGetValue(ty, out service);
            public void CacheService(Type ty, object service)
            {
                _services[ty] = service;
                var disposable = service as IDisposable;
                if (disposable != null) _disposalStack.Push(disposable);
            }

            public void Dispose()
            {
                var exceptions = new List<Exception>();
                while (_disposalStack.Count > 0)
                {
                    try
                    {
                        _disposalStack.Pop().Dispose();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
                if (exceptions.Count == 1) throw exceptions[0];
                if (exceptions.Count > 1) throw new AggregateException(exceptions);
            }
        }
        private Cache _execution = new Cache();
        private Cache _step;

        private readonly IServiceFactory _factory;
        private readonly object _sync = new object();

        public ServiceContext(IServiceFactory factory)
        {
            _factory = factory;
        }

        public TService GetService<TService>()
        {
            lock (_sync)
            {
                var ty = typeof(TService);
                object service;
                if (_step != null && _step.TryGetService(ty, out service)
                    || _execution.TryGetService(ty, out service))
                {
                    return (TService)service;
                }
                var living = _factory.CreateService<TService>(this);
                if (living == null) throw new NotSupportedException($"The service type {ty} is not supported by the service factory");
                switch (living.Value.Lifetime)
                {
                    case ServiceLifetime.ExecutionContext:
                        _execution.CacheService(ty, living.Value.Service);
                        break;
                    case ServiceLifetime.ExecutionStep:
                        _step.CacheService(ty, living.Value.Service);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(living.Value.Lifetime));
                }
                return living.Value.Service;
            }
        }

        public void BeginStep()
        {
            lock (_sync)
            {
                _step?.Dispose();
                _step = new Cache();
            }
        }

        public void EndStep()
        {
            lock (_sync)
            {
                _step?.Dispose();
                _step = null;
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                EndStep();
                _execution?.Dispose();
                _execution = null;
            }
        }
    }
}