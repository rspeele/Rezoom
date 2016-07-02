namespace Data.Resumption.IPGeo
{
    /// <summary>
    /// JSON type used in API response for geolocation info about an IP address.
    /// </summary>
    /// <remarks>
    /// Based on documentation at http://ip-api.com/docs/api:batch#response .
    /// </remarks>
    public class GeoInfo
    {
        public enum StatusType
        {
            Success,
            Fail
        }
        public StatusType Status { get; set; }
        /// <summary>
        /// Error message, if <see cref="Status"/> is <c>Fail</c>.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Country name.
        /// </summary>
        public string Country { get; set; }
        /// <summary>
        /// Country code.
        /// </summary>
        public string CountryCode { get; set; }
        /// <summary>
        /// Region code.
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// Region name.
        /// </summary>
        public string RegionName { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }

        /// <summary>
        /// Latitude.
        /// </summary>
        public double Lat { get; set; }
        /// <summary>
        /// Longitude.
        /// </summary>
        public double Lon { get; set; }

        /// <summary>
        /// Timezone name e.g. <c>"Europe/Amsterdam"</c>.
        /// </summary>
        public string TimeZone { get; set; }
        /// <summary>
        /// Internet service provider name.
        /// </summary>
        public string Isp { get; set; }
        /// <summary>
        /// Organization name.
        /// </summary>
        public string Org { get; set; }
        /// <summary>
        /// AS Number and name.
        /// </summary>
        public string As { get; set; }
        /// <summary>
        /// IP address used for query.
        /// </summary>
        public string Query { get; set; }
    }
}