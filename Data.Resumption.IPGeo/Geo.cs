namespace Data.Resumption.IPGeo
{
    public static class Geo
    {
        /// <summary>
        /// Get geolocation info for <paramref name="ip"/>.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static DataTask<GeoInfo> Locate(string ip) => new GeoRequest(ip).ToDataTask();
    }
}
