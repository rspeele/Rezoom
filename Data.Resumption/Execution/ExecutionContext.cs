using System.Threading.Tasks;

namespace Data.Resumption.Execution
{
    /// <summary>
    /// Handles execution of an <see cref="DataTask{TResult}"/> by stepping through it and running its pending
    /// <see cref="DataRequest"/>s with caching and deduplication.
    /// </summary>
    public class ExecutionContext
    {
        private readonly IExecutionLog _log;
        private readonly ServiceContext _serviceContext;
        private readonly ResponseCache _responseCache = new ResponseCache();

        /// <summary>
        /// Create an execution context by giving it an <see cref="IServiceFactory"/> to provide
        /// services required by the <see cref="DataRequest"/>s that it'll be responsible for executing.
        /// </summary>
        /// <param name="serviceFactory"></param>
        /// <param name="log"></param>
        public ExecutionContext(IServiceFactory serviceFactory, IExecutionLog log = null)
        {
            _serviceContext = new ServiceContext(serviceFactory);
            _log = log;
        }

        private async Task<DataTask<T>> ExecutePending<T>(Step<T> step)
        {
            _log?.OnStepStart();
            _serviceContext.BeginStep();
            Batch<DataResponse> responses;
            try
            {
                var stepContext = new StepContext(_serviceContext, _log, _responseCache);
                var retrievals = step.Pending.MapCS
                    (request => stepContext.AddRequest(request));
                await stepContext.Execute();
                responses = retrievals.MapCS(retrieve => retrieve());
            }
            finally
            {
                _serviceContext.EndStep();
                _log?.OnStepFinish();
            }
            return step.Resume.Invoke(responses);
        }

        /// <summary>
        /// Asynchronously run the given <see cref="DataTask{TResult}"/> to completion.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public async Task<T> Execute<T>(DataTask<T> task)
        {
            while (true)
            {
                var step = task.Step;
                if (step == null)
                {
                    return task.Immediate;
                }
                task = await ExecutePending(step);
            }
        }

        public TService GetService<TService>() => _serviceContext.GetService<TService>();
    }
}
