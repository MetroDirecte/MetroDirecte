using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroDirecte.API
{
    public partial class APIManager
    {
        public async Task<string> GetGTKAsync()
        {
            return await GetAsync("/");
            await GetAsync("/v3/login.awp?gtk=1&v=4.75.0");

            var cookies = filter.CookieManager.GetCookies(
                new Uri(BaseUrl)
            );
            
            foreach (var cookie in cookies)
            {
                if (cookie.Name == "GTK")
                    return cookie.Value;
            }

            throw new Exception("GTK cookie not found");
        }
    }
}