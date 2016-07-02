namespace Data.Resumption.IPGeo
{
    /// <summary>
    /// JSON type used in API request for geolocation info about an IP address.
    /// </summary>
    /// <remarks>
    /// Based on documentation at http://ip-api.com/docs/api:batch#request .
    /// </remarks>
    internal class GeoQuery
    {
        /// <summary>
        /// The IPv4 or IPv6 address to get info about.
        /// </summary>
        public string Query { get; set; }
        /// <summary>
        /// Optional comma-separated selection of fields.
        /// </summary>
        public string Fields { get; set; }
        /// <summary>
        /// Optional language for localization.
        /// </summary>
        public string Lang { get; set; }
    }
}