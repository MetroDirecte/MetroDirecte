using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using MetroDirecte.API;
using Newtonsoft.Json.Linq;
using Windows.Security.Credentials;

namespace MetroDirecte
{
    public sealed partial class MainPage : Page
    {
        private string username;
        private string password;
        private string uuid;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Disabled;

            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= MainPage_Loaded;

            if (!string.IsNullOrWhiteSpace(AuthManager.GetCredential("token")) && !string.IsNullOrWhiteSpace(AuthManager.GetCredential("userdata")))
            {
                ConnectionAttempt.Visibility = Visibility.Visible;

                if (await AuthManager.RenewLogin())
                {
                    ConnectionAttempt.Visibility = Visibility.Collapsed;

                    Frame.Navigate(typeof(Home));
                    Frame.BackStack.Clear();
                    Frame.ForwardStack.Clear();
                    return;
                }

                ConnectionAttempt.Visibility = Visibility.Collapsed;
            }
        }

        private string DecodeBase64(string value)
        {
            byte[] bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private void ShowQcm(JObject qcm)
        {
            QcmPropositions.Children.Clear();
            QcmError.Text = "";

            JToken dataToken = qcm["data"];

            if (dataToken == null || dataToken.Type != JTokenType.Object)
            {
                QcmError.Text = "réponse QCM invalide.";
                LoginButton.IsEnabled = true;
                return;
            }

            JToken questionToken = dataToken["question"];
            JToken propositionsToken = dataToken["propositions"];

            if (questionToken == null || propositionsToken == null)
            {
                QcmError.Text = "données QCM manquantes.";
                LoginButton.IsEnabled = true;
                return;
            }

            QcmQuestion.Text = DecodeBase64(
                questionToken.ToString()
            );

            if (propositionsToken.Type != JTokenType.Array)
            {
                QcmError.Text = "propositions QCM invalides.";
                LoginButton.IsEnabled = true;
                return;
            }

            foreach (JToken proposition in propositionsToken)
            {
                string encoded = proposition.ToString();
                string text = DecodeBase64(encoded);

                Button button = new Button
                {
                    Content = text,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    FontSize = 18,
                    Padding = new Thickness(15, 10, 15, 10),
                    Tag = encoded
                };

                button.Click += QcmProposition_Click;

                QcmPropositions.Children.Add(button);
            }

            QcmOverlay.Visibility = Visibility.Visible;
        }

        private async void QcmProposition_Click(
            object sender,
            RoutedEventArgs e)
        {
            QcmOverlay.Visibility = Visibility.Collapsed;

            Button button = (Button)sender;

            string selectedProposition = button.Tag as string;

            QcmError.Text = "";

            var api = new APIManager();

            string qcmjson = await api.PostAsync(
               "/v3/connexion/doubleauth.awp?verbe=post&v=4.102.1",
               new JObject {
                   { "choix", selectedProposition }
               }
            );

            JObject qcmdata = JObject.Parse(qcmjson);

            int qcmcode = (int)qcmdata["code"];
            if(qcmcode != 200)
            {
                ErrorText.Text = "réponse QCM invalide, veuillez réessayer";
                LoginButton.IsEnabled = true;
                return;
            }

            string cn = (string)qcmdata["data"]["cn"];
            string cv = (string)qcmdata["data"]["cv"];

            api.ResetCacheToken();

            string gtk = await api.GetGTKAsync();

            string json = await api.PostAsync(
                "/v3/login.awp?v=4.102.1",
                new JObject
                {
                    { "identifiant", username },
                    { "motdepasse", password },
                    { "isReLogin", false },
                    { "cn", cn },
                    { "cv", cv },
                    { "uuid", uuid },
                    { "fa", new JArray()
                        {
                            new JObject()
                            {
                                { "cn", cn },
                                { "cv", cv },
                                { "uniq", false }
                            }
                        }
                    }
                },
                null,
                gtk
            );

            JObject data = JObject.Parse(json);

            int code = (int)data["code"];
            string token = (string)data["token"];
            string message = (string)data["message"];

            if (code != 200)
            {
                ErrorText.Text = message.ToLower();
                LoginButton.IsEnabled = true;
                return;
            }

            AuthManager.SaveCredential("cn", cn);
            AuthManager.SaveCredential("cv", cv);

            FinishLogin(data);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(uuid))
                {
                    uuid = AuthManager.GetCredential("uuid");
                    
                    if (string.IsNullOrEmpty(uuid))
                    {
                        uuid = Guid.NewGuid().ToString();
                        AuthManager.SaveCredential("uuid", uuid);
                    }
                }

                AuthManager.DeleteCredential("cn");
                AuthManager.DeleteCredential("cv");

                LoginButton.IsEnabled = false;
                ErrorText.Text = "";

                username = UsernameBox.Text;
                password = PasswordBox.Password;

                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    ErrorText.Text = "veuillez remplir tous les champs.";
                    LoginButton.IsEnabled = true;
                    return;
                }

                var api = new APIManager();

                api.ResetCacheToken();

                string gtk = await api.GetGTKAsync();

                string json = await api.PostAsync(
                    "/v3/login.awp?v=4.102.1",
                    new JObject
                    {
                        { "identifiant", username },
                        { "motdepasse", password },
                        { "sesouvenirdemoi", true },
                        { "isReLogin", false },
                        { "uuid", uuid },
                        { "fa", new JArray() }
                    },
                    null,
                    gtk
                );

                JObject data = JObject.Parse(json);
                
                int code = (int)data["code"];
                string token = (string)data["token"];
                string message = (string)data["message"];

                if (code != 200 && code != 250)
                {
                    ErrorText.Text = message.ToLower();
                    LoginButton.IsEnabled = true;
                    return;
                }

                // QCM
                if (code == 250)
                {
                     string qcmjson = await api.PostAsync(
                        "/v3/connexion/doubleauth.awp?verbe=get&v=4.102.1",
                        new JObject {},
                        token
                    );
                    JObject qcmdata = JObject.Parse(qcmjson);

                    ShowQcm(qcmdata);
                    return;
                }

                FinishLogin(data);
            }
            catch (Exception err)
            {
                LoginButton.IsEnabled = true;
                ErrorText.Text = "une erreur interne s'est produite\n\n" + err.ToString();
            }
        }

        private async void FinishLogin(JObject data)
        {
            try
            {
                AuthManager.SaveCredential("token", (string)data["token"]);
                AuthManager.SaveCredential("userdata", data["data"]["accounts"][0].ToString());

                LoginButton.IsEnabled = true;

                Frame.Navigate(typeof(Home));
                Frame.BackStack.Clear();
                Frame.ForwardStack.Clear();
            }
            catch (Exception err)
            {
                LoginButton.IsEnabled = true;
                ErrorText.Text = "une erreur interne s'est produite\n\n" + err.ToString();
            }
        }
       
    }
}
