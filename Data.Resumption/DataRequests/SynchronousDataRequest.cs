using System;
using System.Threading.Tasks;
using Data.Resumption.Services;

namespace Data.Resumption.DataRequests
{
    /// <summary>
    /// Abstract base class for data requests that do not use TPL tasks for data retrieval.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SynchronousDataRequest<T> : DataRequest<T>
    {
        public abstract Func<T> PrepareSynchronous(IServiceContext context);
        public sealed override Func<Task<T>> Prepare(IServiceContext context)
        {
            var synch = PrepareSynchronous(context);
            return () => Task.FromResult(synch());
        }
    }
}