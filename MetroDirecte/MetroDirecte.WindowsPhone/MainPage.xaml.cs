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

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
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
                return;
            }

            JToken questionToken = dataToken["question"];
            JToken propositionsToken = dataToken["propositions"];

            if (questionToken == null || propositionsToken == null)
            {
                QcmError.Text = "données QCM manquantes.";
                return;
            }

            QcmQuestion.Text = DecodeBase64(
                questionToken.ToString()
            );

            if (propositionsToken.Type != JTokenType.Array)
            {
                QcmError.Text = "propositions QCM invalides.";
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
                return;
            }

            string cn = (string)qcmdata["cn"];
            string cv = (string)qcmdata["cv"];

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
                    { "uuid", "" },
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

            if (code != 200 && code != 250)
            {
                ErrorText.Text = message.ToLower();
                return;
            }

            FinishLogin(token);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ErrorText.Text = "";

                username = UsernameBox.Text;
                password = PasswordBox.Password;

                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    ErrorText.Text = "veuillez remplir tous les champs.";
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
                        { "isReLogin", false },
                        { "uuid", "" },
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

                FinishLogin(token);
            }
            catch (Exception err)
            {
                ErrorText.Text = "une erreur interne s'est produite\n\n" + err.ToString();
            }
        }

        private async void FinishLogin(string token)
        {
            var vault = new PasswordVault();

            vault.Add(new PasswordCredential(
                "MetroDirecte",
                "token",
                token
            ));

            ErrorText.Text = "success";
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }
    }
}
