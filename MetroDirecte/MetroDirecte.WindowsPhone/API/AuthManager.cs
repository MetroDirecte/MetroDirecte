using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Windows.Security.Credentials;

namespace MetroDirecte.API
{
    class AuthManager
    {
        public async static Task<bool> LogOut()
        {
            var api = new APIManager();

            await api.GetAsync("/restv3/ws/user/logout?v=4.102.1");

            DeleteCredential("token");
            DeleteCredential("userdata");

            return true;
        }

        public static JObject GetUserData()
        {
            string rawdata = GetCredential("userdata");

            if (string.IsNullOrWhiteSpace(GetCredential("userdata")))
                throw new Exception("aucune donnée utilisateur trouvée");

            return JObject.Parse(rawdata);
        }

        public async static Task<bool> RenewLogin()
        {
            return true; // JV LE FINIR PLUS TARD SA CLC

            if (
                string.IsNullOrWhiteSpace(GetCredential("token")) ||
                string.IsNullOrWhiteSpace(GetCredential("userdata")) ||
                string.IsNullOrWhiteSpace(GetCredential("uuid"))
            )
                return false;

            var api = new APIManager();

            string json = await api.PostAsync(
                "/v3/login.awp?v=4.102.1",
                new JObject
                {
                    { "identifiant", GetCredential("username") },
                    { "motdepasse", "" },
                    { "isReLogin", true },
                    { "uuid", GetCredential("uuid") },
                    { "fa", new JArray() }
                },
                GetCredential("token"),
                null
            );

            JObject data = JObject.Parse(json);

            int code = (int)data["code"];
            string token = (string)data["token"];
            string message = (string)data["message"];

            return true;
        }

        public static string GetCredential(string key)
        {
            var vault = new PasswordVault();

            try
            {
                var credential = vault.Retrieve("MetroDirecte", key);
                credential.RetrievePassword();

                return credential.Password;
            }
            catch
            {
                return null;
            }
        }

        public static void SaveCredential(string key, string value)
        {
            var vault = new PasswordVault();

            try
            {
                var old = vault.Retrieve("MetroDirecte", key);
                vault.Remove(old);
            }
            catch
            {}

            vault.Add(new PasswordCredential(
                "MetroDirecte",
                key,
                value
            ));
        }

        public static void DeleteCredential(string key)
        {
            var vault = new PasswordVault();

            try
            {
                var credential = vault.Retrieve("MetroDirecte", key);
                vault.Remove(credential);
            }
            catch
            {}
        }
    }
}
