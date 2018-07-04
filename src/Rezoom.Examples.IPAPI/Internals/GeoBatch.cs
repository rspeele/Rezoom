using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rezoom.IPGeo
{
    /// <summary>
    /// Step-local batch of IP addresses for which to request geolocation info.
    /// </summary>
    internal class GeoBatch
    {
        private readonly List<GeoQuery> _queries = new List<GeoQuery>();

        private Task<List<GeoInfo>> _runningTask;

        public Func<CancellationToken, Task<GeoInfo>> Prepare(GeoQuery query)
        {
            if (_runningTask != null)
            {
                throw new InvalidOperationException("Calling Prepare() after the batch has started is nonsense.");
            }
            var index = _queries.Count;
            _queries.Add(query);
            return _ => GetResult(index);
        }

        private static async Task<List<GeoInfo>> GetResults(List<GeoQuery> requests)
        {
            if (requests.Count <= GeoApi.MaxQueriesPerBatch)
            {
                return await GeoApi.QueryBatch(requests);
            }
            var results = new List<GeoInfo>();
            while (requests.Count > 0)
            {
                var chunk = requests.Take(GeoApi.MaxQueriesPerBatch).ToList();
                results.AddRange(await GeoApi.QueryBatch(chunk));
                requests.RemoveRange(0, Math.Min(requests.Count, GeoApi.MaxQueriesPerBatch));
            }
            return results;
        }

        private async Task<GeoInfo> GetResult(int index)
        {
            if (_runningTask == null)
            {
                _runningTask = GetResults(_queries);
            }
            var results = await _runningTask;
            return results[index];
        }
    }
}
