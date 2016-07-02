using System.Threading.Tasks;
using Data.Resumption.Services;

namespace Data.Resumption.Execution
{
    /// <summary>
    /// Handles execution of an <see cref="IDataTask{TResult}"/> by stepping through it and running its pending
    /// <see cref="IDataRequest"/>s with caching and deduplication.
    /// </summary>
    public class ExecutionContext : IServiceContext
    {
        private readonly IExecutionLog _log;
        private readonly ServiceContext _serviceContext;
        private readonly ResponseCache _responseCache = new ResponseCache();

        /// <summary>
        /// Create an execution context by giving it an <see cref="IServiceFactory"/> to provide
        /// services required by the <see cref="IDataRequest"/>s that it'll be responsible for executing.
        /// </summary>
        /// <param name="serviceFactory"></param>
        /// <param name="log"></param>
        public ExecutionContext(IServiceFactory serviceFactory, IExecutionLog log = null)
        {
            _serviceContext = new ServiceContext(serviceFactory);
            _log = log;
        }

        private async Task<IDataTask<T>> ExecutePending<T>(RequestsPending<T> pending)
        {
            _log?.OnStepStart();
            _serviceContext.BeginStep();
            Batch<SuccessOrException> responses;
            try
            {
                var stepContext = new StepContext(_serviceContext, _log, _responseCache);
                var retrievals = pending.Requests.Map
                    (request => stepContext.AddRequest(request));
                await stepContext.Execute();
                responses = retrievals.Map(retrieve => retrieve());
            }
            finally
            {
                _serviceContext.EndStep();
                _log?.OnStepFinish();
            }
            return pending.Resume(responses);
        }

        /// <summary>
        /// Asynchronously run the given <see cref="IDataTask{TResult}"/> to completion.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public async Task<T> Execute<T>(IDataTask<T> task)
        {
            var executing = true;
            var end = default(T);
            while (executing)
            {
                var step = task.Step();
                task = await step.Match
                    ( ExecutePending
                    , result =>
                    {
                        executing = false;
                        end = result;
                        return Task.FromResult<IDataTask<T>>(null);
                    });
            }
            return end;
        }

        public TService GetService<TService>() => _serviceContext.GetService<TService>();
    }
}
