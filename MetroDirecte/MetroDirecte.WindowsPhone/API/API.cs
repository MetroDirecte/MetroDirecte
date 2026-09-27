using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Newtonsoft.Json.Linq;

namespace MetroDirecte.API
{
    public partial class APIManager
    {
        private const string BaseUrl = "https://api.ecoledirecte.com";
        private static string Token = "";
        private static string TFAToken = "";

        private readonly HttpClient client;
        private readonly HttpBaseProtocolFilter filter;

        public APIManager()
        {
            filter = new HttpBaseProtocolFilter();

            filter.CacheControl.ReadBehavior =
                HttpCacheReadBehavior.MostRecent;

            filter.CacheControl.WriteBehavior =
                HttpCacheWriteBehavior.NoCache;

            client = new HttpClient(filter);

            client.DefaultRequestHeaders.UserAgent.Clear();

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:156.0) Gecko/20100101 Firefox/156.0"
            );
        }

        public void ResetCacheToken()
        {
            Token = "";
        }

        public async Task<string> GetAsync(string endpoint, string token = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri(BaseUrl + endpoint)
            );

            if (!string.IsNullOrEmpty(TFAToken))
            {
                request.Headers.Add("2FA-Token", TFAToken);
            }

            if (!string.IsNullOrEmpty(Token))
            {
                request.Headers.Add("X-Token", Token);
            }

            if (!string.IsNullOrEmpty(token))
            {
                Token = token;
                request.Headers.Add("X-Token", token);
            }

            HttpResponseMessage response = await client.SendRequestAsync(request);

            string newToken;

            if (response.Headers.TryGetValue("2fa-token", out newToken))
            {
                TFAToken = newToken;
            }

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> PostAsync(
            string endpoint,
            JObject parameters,
            string token = null,
            string gtk = null
        )
        {
            string json = parameters.ToString(Newtonsoft.Json.Formatting.None);

            var form = new Dictionary<string, string>
            {
                { "data", json }
            };

            var content = new HttpFormUrlEncodedContent(form);

            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(BaseUrl + endpoint)
            );

            request.Content = content;

            if (!string.IsNullOrEmpty(TFAToken))
            {
                request.Headers.Add("2FA-Token", TFAToken);
            }

            if (!string.IsNullOrEmpty(Token))
            {
                request.Headers.Add("X-Token", Token);
            }

            if (!string.IsNullOrEmpty(token))
            {
                Token = token;
                request.Headers.Add("X-Token", token);
            }

            if (!string.IsNullOrEmpty(gtk))
            {
                request.Headers.Add("X-Gtk", gtk);
            }

            HttpResponseMessage response = await client.SendRequestAsync(request);

            string newToken;

            if (response.Headers.TryGetValue("2fa-token", out newToken))
            {
                TFAToken = newToken;
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
