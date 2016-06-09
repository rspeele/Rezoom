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
        private readonly ResponseCache _cache;
        private readonly List<Func<Task>> _unsequenced = new List<Func<Task>>();
        private readonly Dictionary<object, List<Func<Task>>> _sequenceGroups
            = new Dictionary<object, List<Func<Task>>>();
        private readonly Dictionary<object, Func<SuccessOrException>> _deduped
            = new Dictionary<object, Func<SuccessOrException>>();

        public StepContext(IServiceContext serviceContext, ResponseCache cache)
        {
            _serviceContext = serviceContext;
            _cache = cache;
        }

        private Func<SuccessOrException> AddRequestToRun(IDataRequest request)
        {
            if (request.Mutation)
            {
                _cache.Invalidate(request.DataSource);
            }
            var eventual = default(SuccessOrException);
            Func<SuccessOrException> getEventual = () => eventual;
            Func<Task<object>> prepared;
            try
            {
                prepared = request.Prepare(_serviceContext);
            }
            catch (Exception ex)
            {
                return () => new SuccessOrException(ex);
            }
            Func<Task> run = async () =>
            {
                try
                {
                    var success = await prepared();
                    eventual = new SuccessOrException(success);
                }
                catch (Exception ex)
                {
                    eventual = new SuccessOrException(ex);
                }
            };
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
            return getEventual;
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
            var sequenceGroupTasks = _sequenceGroups.Values.Select(ExecuteSequentialGroup);
            var tasks = _unsequenced.Select(f => f()).Concat(sequenceGroupTasks);
            await Task.WhenAll(tasks);
        }
    }
}