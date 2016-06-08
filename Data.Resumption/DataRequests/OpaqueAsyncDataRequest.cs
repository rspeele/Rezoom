using System;
using System.Threading.Tasks;

namespace Data.Resumption.DataRequests
{
    internal class OpaqueAsyncDataRequest<T> : BaseDataRequest<T>
    {
        private readonly Func<Task<T>> _task;

        public OpaqueAsyncDataRequest(Func<Task<T>> task)
        {
            _task = task;
        }

        public override Func<Task<object>> Prepare(IServiceContext context)
            => async () => await _task();
    }
}
