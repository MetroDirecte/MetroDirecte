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

        private async void LogoutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await AuthManager.LogOut();

            Frame.Navigate(typeof(MainPage));
            Frame.BackStack.Clear();
            Frame.ForwardStack.Clear();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (string.IsNullOrWhiteSpace(AuthManager.GetCredential("token")) ||
                string.IsNullOrWhiteSpace(AuthManager.GetCredential("userdata")))
            {
                Frame.Navigate(typeof(MainPage));
                return;
            }

            JObject userData = AuthManager.GetUserData();

            Username.Text = (string)userData["identifiant"];
        }
    }
}
