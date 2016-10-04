using CaregiverMobile.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class LoginPage : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

        int resultLogin = 0;
        public LoginPage()
        {
            this.InitializeComponent();
        }



        private async void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (UserName.Text != "" && PassWord.Password != "")
            {
                loginRestCall(UserName.Text, PassWord.Password);
            }
            else
            {
                MessageDialog md = new MessageDialog("Please complete the input...");
                await md.ShowAsync();
            }
        }

        private async void loginRestCall(string username, string password)
        {
            login_progressring.IsActive = true;
            var login = new UserModel()
            {
                username = username,
                password = password
            };

            try
            {
                Common common = new Common();
                var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(common.getIP() + "api/login", login);

                //will throw an exception if not successful
                //response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    login_progressring.IsActive = false;
                    string content = await response.Content.ReadAsStringAsync();
                    //result.Text = content;
                    UserModel userModel = JsonConvert.DeserializeObject<UserModel>(content);
                    if (userModel != null)
                    {
                        settings.Values["api"] = userModel.api;
                        settings.Values["userid"] = userModel._id;
                        settings.Values["userrole"] = userModel.userrole;
                        settings.Values["login_status"] = true;
                    }
                    resultLogin = 1;

                    //phone device biometric demo
                    //this.Frame.Navigate(typeof(Enrollment1), userModel);

                    //moible device
                    //this.Frame.Navigate(typeof(Enrollment1), userModel);

                    //test case
                    //if (userModel.userrole == "elderly")
                    //    this.Frame.Navigate(typeof(Elderly_1_Page));
                    //else
                    //    this.Frame.Navigate(typeof(MainPage));

                    ///////////////////////////////////

                    object enroll = settings.Values["enroll"];
                    if (enroll == null)
                    {
                        this.Frame.Navigate(typeof(Enrollment1), userModel);
                    }
                    else
                    {
                        if ((bool)enroll == true)
                        {
                            if (userModel.userrole == "elderly")
                                this.Frame.Navigate(typeof(Elderly_1_Page));
                            else
                                this.Frame.Navigate(typeof(MainPage));
                        }
                        else
                        {
                            this.Frame.Navigate(typeof(Enrollment1), userModel);
                        }

                    }






                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    login_progressring.IsActive = false;
                    MessageDialog md = new MessageDialog("Username or password incorrect!");
                    await md.ShowAsync();
                }
                else
                {
                    login_progressring.IsActive = false;
                    MessageDialog md = new MessageDialog("Failed to login! Try again later.");
                    await md.ShowAsync();
                }
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine("Exception login : " + e);
                login_progressring.IsActive = false;
                MessageDialog md = new MessageDialog("Failed to login! Try again later.");
                await md.ShowAsync();
            }

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (resultLogin == 1)
                Frame.BackStack.RemoveAt(Frame.BackStackDepth - 1);
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("clicked");
            this.Frame.Navigate(typeof(SettingsPage), "login");
        }
    }

}
