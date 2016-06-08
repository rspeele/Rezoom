using System;
using System.Threading.Tasks;

namespace Data.Resumption.DataRequests
{
    public abstract class BaseDataRequest<T> : IDataRequest<T>
    {
        public virtual IComparable Identity => null;
        public virtual IComparable DataSource => null;
        public virtual IComparable SequenceGroup => null;
        public virtual bool Idempotent => false;
        public virtual bool Mutation => true;
        public abstract Func<Task<object>> Prepare(IServiceContext context);
    }
}