using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace MetroDirecte.Controls
{
    public sealed partial class BottomBar : UserControl
    {
        public BottomBar()
        {
            this.InitializeComponent();
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            Navigate(typeof(Home));
        }

        private void Calendar_Click(object sender, RoutedEventArgs e)
        {
            Navigate(typeof(Home));
        }

        private void Grades_Click(object sender, RoutedEventArgs e)
        {
            Navigate(typeof(Home));
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Navigate(typeof(Settings));
        }

        private async void Navigate(Type pageType)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                await ShowNavigationError();
                return;
            }

            bool success = rootFrame.Navigate(pageType);

            if (!success)
            {
                await ShowNavigationError();
            }
        }

        private async Task ShowNavigationError()
        {
            var dialog = new MessageDialog(
                "impossible d'ouvrir cette page",
                "erreur de navigation");

            dialog.Commands.Add(
                new UICommand("OK"));

            await dialog.ShowAsync();
        }
    }
}