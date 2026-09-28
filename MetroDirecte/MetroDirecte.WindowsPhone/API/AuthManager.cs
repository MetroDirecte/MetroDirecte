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
            if (
                string.IsNullOrWhiteSpace(GetCredential("token")) ||
                string.IsNullOrWhiteSpace(GetCredential("userdata")) ||
                string.IsNullOrWhiteSpace(GetCredential("uuid"))
            )
                return false;

            JObject userData = GetUserData();

            var api = new APIManager();

            api.ResetCacheToken();

            string gtk = await api.GetGTKAsync();

            JObject reqdata = new JObject
            {
                { "identifiant", userData["identifiant"] },
                { "motdepasse", "" },
                { "isReLogin", true },
                { "uuid", GetCredential("uuid") },
                { "typeCompte", userData["typeCompte"] },
                { "accesstoken", userData["access_token"] }
            };

            if(!string.IsNullOrWhiteSpace(GetCredential("cn")) && !string.IsNullOrWhiteSpace(GetCredential("cv")))
            {
                reqdata["cn"] = GetCredential("cn");
                reqdata["cv"] = GetCredential("cv");
                reqdata["fa"] =
                    new JArray()
                    {
                        new JObject()
                        {
                            { "cn", GetCredential("cn") },
                            { "cv", GetCredential("cv") },
                            { "uniq", false }
                        }
                    };
            }
            else
            {
                reqdata["fa"] = new JArray();
            }

            string json = await api.PostAsync(
                "/v3/login.awp?v=4.102.1",
                reqdata,
                GetCredential("token"),
                gtk
            );

            JObject data = JObject.Parse(json);

            int code = (int)data["code"];

            if (code != 200)
                return false;

            SaveCredential("token", (string)data["token"]);
            SaveCredential("userdata", data["data"]["accounts"][0].ToString());

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
