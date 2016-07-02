using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Data.Resumption.IPGeo
{
    /// <summary>
    /// Helper method to query the IP geolocation API at http://ip-api.com .
    /// </summary>
    /// <remarks>
    /// Based on documentation at http://ip-api.com/docs/api:batch .
    /// </remarks>
    internal static class GeoApi
    {
        private const string ApiUrl = "http://ip-api.com/batch";
        public const int MaxQueriesPerBatch = 100;
        public static async Task<List<GeoInfo>> QueryBatch(List<GeoQuery> queries)
        {
            if (queries.Count > MaxQueriesPerBatch) throw new Exception("Too many queries in request");
            var request = JsonConvert.SerializeObject(queries, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            });
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(ApiUrl, new StringContent(request));
                if (!response.IsSuccessStatusCode) throw new Exception("Request failed: " + response.StatusCode);
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<GeoInfo>>(responseJson);
            }
        }
    }
}
