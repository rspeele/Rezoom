using System;
using System.Threading.Tasks;

namespace Rezoom.IPGeo.Internals
{
    internal class GeoCacheInfo : CacheInfo
    {
        private readonly string _ip;
        public GeoCacheInfo(string ip)
        {
            _ip = ip;
        }
        public override object Category => typeof(GeoCacheInfo).Assembly;
        public override object Identity => _ip;
    }
    /// <summary>
    /// Implements <see cref="Errand"/> for looking up <see cref="GeoInfo"/> for an IP address.
    /// </summary>
    internal class GeoErrand : CS.AsynchronousErrand<GeoInfo>
    {
        private readonly string _ip;

        public GeoErrand(string ip)
        {
            _ip = ip;
        }

        public override CacheInfo CacheInfo => new GeoCacheInfo(_ip);

        public override Func<Task<GeoInfo>> Prepare(ServiceContext context)
        {
            var batch = context.GetService<StepLocal<GeoBatch>>().Service;
            return batch.Prepare(new GeoQuery { Query = _ip });
        }
    }
}
