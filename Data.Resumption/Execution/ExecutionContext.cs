﻿using System.Threading.Tasks;
using Data.Resumption.Services;

namespace Data.Resumption.Execution
{
    public class ExecutionContext : IServiceContext
    {
        private readonly ServiceContext _serviceContext;
        private readonly ResponseCache _responseCache = new ResponseCache();

        public ExecutionContext(IServiceFactory serviceFactory)
        {
            _serviceContext = new ServiceContext(serviceFactory);
        }

        private async Task<IDataTask<T>> ExecutePending<T>(RequestsPending<T> pending)
        {
            _serviceContext.BeginStep();
            Batch<SuccessOrException> responses;
            try
            {
                var stepContext = new StepContext(_serviceContext, _responseCache);
                var retrievals = pending.Requests.Map
                    (request => stepContext.AddRequest(request));
                await stepContext.Execute();
                responses = retrievals.Map(retrieve => retrieve());
            }
            finally
            {
                _serviceContext.EndStep();
            }
            return pending.Resume(responses);
        }

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