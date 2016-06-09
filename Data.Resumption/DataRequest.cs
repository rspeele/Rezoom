using System;
using System.Threading.Tasks;
using Data.Resumption.Services;

namespace Data.Resumption
{
    /// <summary>
    /// Abstract base class to help define data requests.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DataRequest<T> : IDataRequest<T>
    {
        public virtual object Identity => null;
        public virtual object DataSource => null;
        public virtual object SequenceGroup => null;
        public virtual bool Idempotent => false;
        public virtual bool Mutation => true;
        public abstract Func<Task<T>> Prepare(IServiceContext context);
        Func<Task<object>> IDataRequest.Prepare(IServiceContext context)
        {
            var prepared = Prepare(context);
            return async () => await prepared();
        }
    }
}