using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace MetroDirecte.API
{
    public partial class APIManager
    {
        private const string BaseUrl = "https://api.ecoledirecte.com";

        private readonly HttpClient client;
        private readonly HttpBaseProtocolFilter filter;

        public APIManager()
        {
            filter = new HttpBaseProtocolFilter();
            client = new HttpClient(filter);
        }

        public async Task<string> GetAsync(string endpoint)
        {
            HttpResponseMessage response = await client.GetAsync(
                new Uri(BaseUrl + endpoint)
            );

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> PostAsync(
            string endpoint,
            Dictionary<string, string> parameters)
        {
            var content = new HttpFormUrlEncodedContent(parameters);

            HttpResponseMessage response = await client.PostAsync(
                new Uri(BaseUrl + endpoint),
                content
            );

            return await response.Content.ReadAsStringAsync();
        }
    }
}
