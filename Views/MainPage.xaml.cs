using CaregiverMobile.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.PushNotifications;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CaregiverMobile.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();

        ObservableCollection<TrackListModel> items;
        List<TrackListModel> trackListModel;

        PushNotificationChannel channel = null;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

            //var verified = settings.Values["verified"];

            //if ((bool)verified == false)
            //{
            //    this.Frame.Navigate(typeof(Views.VerifyFace));
            //}
            //else
            //{
            loadElderlyList();
            UpdateChannelUri();
            updateLastLogin();
            //}
        }

        private void OnPushNotification(PushNotificationChannel sender, PushNotificationReceivedEventArgs e)
        {
            String notificationContent = String.Empty;

            switch (e.NotificationType)
            {
                case PushNotificationType.Badge:
                    notificationContent = e.BadgeNotification.Content.GetXml();
                    Debug.WriteLine(notificationContent);
                    break;

                case PushNotificationType.Tile:
                    notificationContent = e.TileNotification.Content.GetXml();
                    Debug.WriteLine(notificationContent);

                    break;

                case PushNotificationType.Toast:
                    notificationContent = e.ToastNotification.Content.GetXml();
                    //Title.Text = notificationContent;
                    Debug.WriteLine(notificationContent);

                    break;

                case PushNotificationType.Raw:
                    notificationContent = e.RawNotification.Content;
                    Debug.WriteLine(notificationContent);
                    break;
            }

            e.Cancel = true;
        }

        private async void UpdateChannelUri()
        {

            object api = settings.Values["api"];
            object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                    //channel.PushNotificationReceived += OnPushNotification;
                    Channel model = new Channel
                    {
                        uri = channel.Uri
                    };

                    try
                    {
                        HttpClient httpClient = new HttpClient();
                        var response = await httpClient.PostAsJsonAsync(common.getIP() + "api/updatechanneluri/" + api.ToString() + "/" + id.ToString(), model);

                        if (response.IsSuccessStatusCode)
                        {
                            Debug.WriteLine("channel uri sent.");
                        }
                        else
                        {
                            MessageDialog md = new MessageDialog("Update channel uri error.");
                            await md.ShowAsync();
                        }
                    }
                    catch
                    {
                        MessageDialog md = new MessageDialog("Update channel uri error.");
                        await md.ShowAsync();
                    }


                }
                catch (Exception ex)
                {
                    Debug.WriteLine("channel uri:" + ex.Message);
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

        private async void loadElderlyList()
        {
            items = new ObservableCollection<TrackListModel>();

            load_elderly_progressbar.Visibility = Visibility.Visible;
            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        load_elderly_progressbar.Visibility = Visibility.Collapsed;
                        string content = await response.Content.ReadAsStringAsync();
                        trackListModel = JsonConvert.DeserializeObject<List<TrackListModel>>(content);


                        foreach (var model in trackListModel)
                        {
                            Debug.WriteLine(model.elderlyid.userfullname + "," + model.elderlyid.alert);
                            //if (model.status == true)
                            //    model.statusString = "Active";
                            //else
                            //    model.statusString = "Inactive";
                            if (model.elderlyid.userprofilepic != null)
                                model.elderlyid.userprofilepic = new Uri(common.getIP() + "images/profile/" + model.elderlyid.userprofilepic);
                            else
                                model.elderlyid.userprofilepic = new Uri(common.getIP() + "images/profile/null");

                            if (model.elderlyid.alert == true)
                                model.elderlyid.alertstring = "Visible";
                            else
                                model.elderlyid.alertstring = "Collapsed";
                            
                            //rule based
                            if (model.indoor != null && model.outdoor != null)
                            {
                                //Debug.WriteLine(model.outdoor.timestamp.ToLocalTime());
                                //Debug.WriteLine(DateTime.Now);

                                if (model.indoor.timestamp.ToLocalTime() > model.outdoor.timestamp.ToLocalTime())
                                {
                                    DateTime current = DateTime.Now;
                                    TimeSpan different = current - model.indoor.timestamp.ToLocalTime();

                                    if (different.TotalMinutes < 60)//1hour
                                    {
                                        model.status = Math.Round(different.TotalMinutes) + "minute(s) ago";
                                        model.statuscolor = "Green";
                                    }
                                    else if (different.TotalMinutes > 60 && different.TotalMinutes < 180)//3hour
                                    {
                                        model.status = "active less than 3 hours";
                                        model.statuscolor = "Orange";

                                    }
                                    else
                                    {
                                        model.status = "not active more than 3 hours";
                                        model.statuscolor = "Red";
                                    }
                                }
                                else
                                {
                                    DateTime current = DateTime.Now;
                                    TimeSpan different = current - model.outdoor.timestamp.ToLocalTime();
                                    //Debug.WriteLine(different.TotalMinutes);

                                    if (different.TotalMinutes < 60)//1hour
                                    {
                                        model.status = Math.Round(different.TotalMinutes) + "minute(s) ago";
                                        model.statuscolor = "Green";
                                    }
                                    else if (different.TotalMinutes > 60 && different.TotalMinutes < 180)//3hour
                                    {
                                        model.status = "active less than 3 hours";
                                        model.statuscolor = "Orange";

                                    }
                                    else
                                    {
                                        model.status = "not active more than 3 hours";
                                        model.statuscolor = "Red";
                                    }
                                }
                            }
                            else if (model.indoor != null && model.outdoor == null)
                            {
                                DateTime current = DateTime.Now;
                                TimeSpan different = current - model.indoor.timestamp.ToLocalTime();

                                if (different.TotalMinutes < 60)//1hour
                                {
                                    model.status = Math.Round(different.TotalMinutes) + "minute(s) ago";
                                    model.statuscolor = "Green";
                                }
                                else if (different.TotalMinutes > 60 && different.TotalMinutes < 180)//3hour
                                {
                                    model.status = "active less than 3 hours";
                                    model.statuscolor = "Orange";

                                }
                                else
                                {
                                    model.status = "not active more than 3 hours";
                                    model.statuscolor = "Red";
                                }

                            }
                            else if (model.indoor == null && model.outdoor != null)
                            {
                                DateTime current = DateTime.Now;
                                TimeSpan different = current - model.outdoor.timestamp.ToLocalTime();

                                if (different.TotalMinutes < 60)//1hour
                                {
                                    model.status = Math.Round(different.TotalMinutes) + "minute(s) ago";
                                    model.statuscolor = "Green";
                                }
                                else if (different.TotalMinutes > 60 && different.TotalMinutes < 180)//3hour
                                {
                                    model.status = "active less than 3 hours";
                                    model.statuscolor = "Orange";

                                }
                                else
                                {
                                    model.status = "not active more than 3 hours";
                                    model.statuscolor = "Red";
                                }
                            }

                            items.Add(model);
                        }
                        ElderlyLvBinding.ItemsSource = items;

                    }
                    else
                    {
                        load_elderly_progressbar.Visibility = Visibility.Collapsed;
                        MessageDialog md = new MessageDialog("Error occur loading elderly list. Try again later.");
                        await md.ShowAsync();
                    }
                }
                catch
                {
                    load_elderly_progressbar.Visibility = Visibility.Collapsed;
                    MessageDialog md = new MessageDialog("Error occur loading elderly list. Try again later.");
                    await md.ShowAsync();
                }

            }
            else
            {
                //api error/null
                MessageDialog md = new MessageDialog("Api error occur. Relogin application.");
                await md.ShowAsync();

                settings.Values["login_status"] = false;
                Application.Current.Exit();
            }
        }

        private void ElderlyButton_Click(object sender, RoutedEventArgs e)
        {
            string tracklist_id = ((Button)sender).CommandParameter.ToString();
            //TrackListModel trackListModel = (from t in items where t._id == tracklist_id select t).First();

            foreach (var item in trackListModel)
            {
                if (item._id == tracklist_id)
                {
                    this.Frame.Navigate(typeof(ElderlyPage), tracklist_id);

                }
            }

        }

        private void menuBtn_Click(object sender, RoutedEventArgs e)
        {
            splitview.IsPaneOpen = !splitview.IsPaneOpen;
        }


        private void homeBtn_Click(object sender, RoutedEventArgs e)
        {
            loadElderlyList();
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private void profileBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ProfilePage));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.Frame.BackStack.Clear();
        }
        
        private async void updateLastLogin()
        {

            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/updatelogin/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {

                    }

                }
                catch
                {

                }
            }
        }
    }
}
