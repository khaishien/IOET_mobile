using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CaregiverMobile.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            String fromPage = e.Parameter as String;
            if(fromPage == "login")
            {
                menuBtn.Visibility = Visibility.Collapsed;
                Pane.Visibility = Visibility.Collapsed;
                LogoutBtn.Visibility = Visibility.Collapsed;

                backBtn.Visibility = Visibility.Visible;
            }
            else if(fromPage == "elderly")
            {
                menuBtn.Visibility = Visibility.Collapsed;
                Pane.Visibility = Visibility.Collapsed;
                backBtn.Visibility = Visibility.Visible;
            }
        }

        private void menuBtn_Click(object sender, RoutedEventArgs e)
        {
            splitview.IsPaneOpen = !splitview.IsPaneOpen;
        }

        private void homeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void profileBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ProfilePage));
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void submitIpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ipaddress.Text != "")
            {
                common.saveIP(ipaddress.Text);
                MessageDialog md = new MessageDialog("Updated IP address.");
                await md.ShowAsync();
            }
        }

        private async void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog md = new MessageDialog("Are you sure to logout?");
            md.Commands.Add(new UICommand { Label = "Yes", Id = 0 });
            md.Commands.Add(new UICommand { Label = "No", Id = 1 });
            var res = await md.ShowAsync();

            if ((int)res.Id == 0)
            {
                settings.Values["login_status"] = false;
                settings.Values["enroll"] = false;
                Application.Current.Exit();
            }
        }

        private void backBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ipaddress.Text = common.getIP();//preload ip

        }
    }


}
