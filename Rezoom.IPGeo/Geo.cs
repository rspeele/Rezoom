using Microsoft.FSharp.Core;
using Rezoom.IPGeo.Internals;
using Rezoom.CS;

namespace Rezoom.IPGeo
{
    public static class Geo
    {
        /// <summary>
        /// Get geolocation info for <paramref name="ip"/>.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static FSharpFunc<Unit, PlanState<GeoInfo>> Locate(string ip) => new GeoErrand(ip).ToPlan();
    }
}
