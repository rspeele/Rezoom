using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Resumption.Services;

namespace Data.Resumption.Execution
{
    internal class StepContext
    {
        private readonly IServiceContext _serviceContext;
        private readonly IExecutionLog _executionLog;
        private readonly ResponseCache _cache;
        private readonly List<Func<Task>> _unsequenced = new List<Func<Task>>();
        private readonly Dictionary<object, List<Func<Task>>> _sequenceGroups
            = new Dictionary<object, List<Func<Task>>>();
        private readonly Dictionary<object, Func<SuccessOrException>> _deduped
            = new Dictionary<object, Func<SuccessOrException>>();

        public StepContext(IServiceContext serviceContext, IExecutionLog executionLog, ResponseCache cache)
        {
            _serviceContext = serviceContext;
            _executionLog = executionLog;
            _cache = cache;
        }

        private class PendingResult
        {
            private SuccessOrException _result;
            public SuccessOrException Get() => _result;
            public async Task Run(IDataRequest request, IExecutionLog log, Func<Task<object>> prepared)
            {
                try
                {
                    _result = new SuccessOrException(await prepared());
                }
                catch (Exception ex)
                {
                    _result = new SuccessOrException(ex);
                }
                log?.OnComplete(request, _result);
            }
        }

        private Func<SuccessOrException> AddRequestToRun(IDataRequest request)
        {
            if (request.Mutation)
            {
                _cache.Invalidate(request.DataSource);
            }
            var eventual = new PendingResult();
            Func<Task<object>> prepared;
            try
            {
                prepared = request.Prepare(_serviceContext);
                _executionLog?.OnPrepare(request);
            }
            catch (Exception ex)
            {
                _executionLog?.OnPrepareFailure(ex);
                return () => new SuccessOrException(ex);
            }
            Func<Task> run = async () => await eventual.Run(request, _executionLog, prepared);
            var sequenceGroupId = request.SequenceGroup;
            if (sequenceGroupId == null)
            {
                _unsequenced.Add(run);
            }
            else
            {
                List<Func<Task>> sequenceGroup;
                if (!_sequenceGroups.TryGetValue(sequenceGroupId, out sequenceGroup))
                {
                    sequenceGroup = new List<Func<Task>>();
                    _sequenceGroups[sequenceGroupId] = sequenceGroup;
                }
                sequenceGroup.Add(run);
            }
            return eventual.Get;
        }

        public Func<SuccessOrException> AddRequest(IDataRequest request)
        {
            var identity = request.Identity;
            // If this request is not cachable, we have to run it.
            if (!request.Idempotent || identity == null) return AddRequestToRun(request);
            // Otherwise...
            var dataSource = request.DataSource;
            // Check for a cached result.
            var cached = _cache.CheckCache(dataSource, identity);
            if (cached != null) return () => cached.Value;
            // Check for de-duplication of this request within this step.
            Func<SuccessOrException> existing;
            if (_deduped.TryGetValue(identity, out existing)) return existing;
            // Otherwise, we really need to run this request.
            var toRun = AddRequestToRun(request);
            _deduped[identity] = toRun;
            return () =>
            {
                var result = toRun();
                _cache.Store(dataSource, identity, result);
                return result;
            };
        }

        private static async Task ExecuteSequentialGroup(IEnumerable<Func<Task>> tasks)
        {
            foreach (var task in tasks) await task();
        }

        public async Task Execute()
        {
            var tasks = new Task[_sequenceGroups.Values.Count + _unsequenced.Count];
            var i = 0;
            foreach (var sgroup in _sequenceGroups.Values)
            {
                tasks[i++] = ExecuteSequentialGroup(sgroup);
            }
            foreach (var unseq in _unsequenced)
            {
                tasks[i++] = unseq();
            }
            if (tasks.Length == 1)
                await tasks[0];
            else
                await Task.WhenAll(tasks);
        }
    }
}