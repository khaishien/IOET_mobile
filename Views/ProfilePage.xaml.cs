using CaregiverMobile.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class ProfilePage : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();


        public ProfilePage()
        {
            this.InitializeComponent();
        }

        private async void LoadProfile()
        {
            load_profile_progressbar.Visibility = Visibility.Visible;
            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];

            if (api != null && id != null)
            {
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/profile/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        load_profile_progressbar.Visibility = Visibility.Collapsed;
                        string content = await response.Content.ReadAsStringAsync();
                        UserModel userModel = JsonConvert.DeserializeObject<UserModel>(content);

                        string format = "dd MMM yyyy";
                        UserProfilePic.UriSource = new Uri(common.getIP() + "images/profile/" + userModel.userprofilepic);
                        UserName.Text = userModel.userfullname;
                        UserJoinSince.Text = userModel.created_at.ToString(format);
                        UserLastUpdated.Text = userModel.updated_at.ToString(format);
                        UserContact.Text = userModel.usercontact;
                        UserEmail.Text = userModel.useremail;
                        UserAddress.Text = userModel.useraddress + " , " + userModel.userpostcode 
                            + " , " + userModel.userstate + " , " + userModel.usercountry;

                    }
                    else
                    {
                        showDialog();
                    }
                }
                catch
                {
                    showDialog();
                }
            }
            else
            {
                MessageDialog md = new MessageDialog("Api error occur. Relogin application.");
                await md.ShowAsync();

                settings.Values["login_status"] = false;
                Application.Current.Exit();
            }

        }

        private async void showDialog()
        {
            load_profile_progressbar.Visibility = Visibility.Collapsed;
            MessageDialog md = new MessageDialog("Error occur loading profile. Try again later.");

            md.Commands.Add(new UICommand { Label = "Try again", Id = 0 });
            md.Commands.Add(new UICommand { Label = "Close", Id = 1 });
            var res = await md.ShowAsync();

            if ((int)res.Id == 0)
            {
                LoadProfile();
            }
            else
            {
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
                else
                {
                    this.Frame.Navigate(typeof(MainPage));
                }
            }
        }

        private void ProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadProfile();
        }

        private void menuBtn_Click(object sender, RoutedEventArgs e)
        {
            splitview.IsPaneOpen = !splitview.IsPaneOpen;
        }


        private void homeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private void profileBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadProfile();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProfile();

        }
    }
}
