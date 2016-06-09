using System;
using System.Threading.Tasks;
using Data.Resumption.Services;

namespace Data.Resumption
{
    internal class OpaqueAsyncDataRequest<T> : DataRequest<T>
    {
        private readonly Func<Task<T>> _task;

        public OpaqueAsyncDataRequest(Func<Task<T>> task)
        {
            _task = task;
        }

        public override Func<Task<T>> Prepare(IServiceContext context)
            => () => _task();
    }
}
