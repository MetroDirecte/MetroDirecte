using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using MetroDirecte.API;
using Newtonsoft.Json.Linq;
using Windows.Security.Credentials;

namespace MetroDirecte
{
    public sealed partial class Home : Page
    {
        public Home()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Disabled;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AuthManager.GetCredential("token")) ||
                string.IsNullOrWhiteSpace(AuthManager.GetCredential("userdata")))
            {
                Frame.Navigate(typeof(MainPage));
                return;
            }

            if (!await AuthManager.RenewLogin())
            {
                Frame.Navigate(typeof(MainPage));
                return;
            }

            JObject userData = AuthManager.GetUserData();

            Username.Text = (string)userData["identifiant"];
        }
    }
}
