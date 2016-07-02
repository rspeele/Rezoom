using System;
using System.Threading.Tasks;
using Data.Resumption.DataRequests;
using Data.Resumption.Services;

namespace Data.Resumption.IPGeo
{
    /// <summary>
    /// Implements <see cref="IDataRequest"/> for looking up <see cref="GeoInfo"/> for an IP address.
    /// </summary>
    internal class GeoRequest : DataRequest<GeoInfo>
    {
        private readonly string _ip;

        public GeoRequest(string ip)
        {
            _ip = ip;
        }

        // Requests for the same ip can be deduped/cached.
        public override object Identity => _ip;
        // The cache will only be cleared when there is a mutation with the same datasource.
        public override object DataSource => typeof(GeoBatch);
        // Requests with the same sequence group will be prepared and executed sequentially, so GeoBatch doesn't need
        // to be thread-safe.
        public override object SequenceGroup => typeof(GeoBatch);
        // Looking up an IP 3 times is the same as looking it up once and returning the result 3 times.
        public override bool Idempotent => true;
        // Looking up an IP doesn't change anything, so it shouldn't invalidate any caches.
        public override bool Mutation => false;

        public override Func<Task<GeoInfo>> Prepare(IServiceContext context)
        {
            var batch = context.GetService<StepLocal<GeoBatch>>().Service;
            return batch.Prepare(new GeoQuery { Query = _ip });
        }
    }
}
