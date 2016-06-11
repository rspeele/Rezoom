using System;
using System.Data.Entity;
using System.Threading.Tasks;
using Data.Resumption.DataRequests;
using Data.Resumption.Services;

namespace Data.Resumption.EF
{
    public abstract class ContextRequest<TContext, T> : DataRequest<T>
        where TContext : DbContext
    {
        public override object DataSource => typeof(TContext);
        public override object SequenceGroup => typeof(TContext);

        protected abstract Func<Task<T>> Prepare(TContext db);

        public sealed override Func<Task<T>> Prepare(IServiceContext context)
        {
            var db = context.GetService<TContext>();
            return Prepare(db);
        }
    }
}