using CaregiverMobile.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class Elderly_SendRequestPage : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();
        CaregiverMonitorModel model;

        public Elderly_SendRequestPage()
        {
            this.InitializeComponent();
            LoadCaregiverList();
        }

        private async void LoadCaregiverList()
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];

            if (api != null && id != null)
            {
                monitor_progressbar.Visibility = Visibility.Visible;
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/caregiverlist/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        model = JsonConvert.DeserializeObject<CaregiverMonitorModel>(content);

                        foreach (var request in model.requests)
                        {
                            model.users.RemoveAll(x => x._id == request.caregiverid._id);
                        }

                        foreach (var tracklist in model.tracklists)
                        {
                            model.users.RemoveAll(x => x._id == tracklist.caregiverid._id);
                        }


                        foreach (var m in model.users)
                        {
                            if (m.active == true)
                            {
                                m.activestring = "Activated";
                            }
                            else
                            {
                                if (m.username == "admin")
                                {
                                    m.activestring = "Activated";
                                }
                                else
                                {
                                    m.activestring = "Not Activated";
                                }
                            }
                        }
                        monitor_progressbar.Visibility = Visibility.Collapsed;

                        CaregiverlvBinding.ItemsSource = model.users;



                    }
                    else
                    {
                        monitor_progressbar.Visibility = Visibility.Collapsed;
                        MessageDialog md = new MessageDialog("Error occur loading monitor list. Try again later.");
                        await md.ShowAsync();
                    }
                }
                catch
                {

                }
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void CaregiverlvBinding_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserModel model = (UserModel)e.AddedItems[0];



            if (model != null)
            {
                if (model.active == true)
                {
                    MessageDialog md = new MessageDialog("Are you sure to send request to caregiver " + model.userfullname);

                    md.Commands.Add(new UICommand { Label = "Yes", Id = 0 });
                    md.Commands.Add(new UICommand { Label = "No", Id = 1 });
                    var res = await md.ShowAsync();

                    if ((int)res.Id == 0)
                    {
                        SendRequest(model._id);
                    }
                }
                else
                {
                    MessageDialog md = new MessageDialog("Caregiver " + model.userfullname + " is not activated yet.");
                    await md.ShowAsync();
                }


            }
        }

        private async void SendRequest(string _id)
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];

            if (api != null && id != null)
            {

                monitor_progressbar.Visibility = Visibility.Visible;
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/sendrequest/" + api.ToString() + "/" + id.ToString() + "/" + _id);

                    if (response.IsSuccessStatusCode)
                    {
                        //string content = await response.Content.ReadAsStringAsync();
                        monitor_progressbar.Visibility = Visibility.Collapsed;
                        MessageDialog md = new MessageDialog("Send request successful.");
                        await md.ShowAsync();

                        if (Frame.CanGoBack)
                        {
                            Frame.GoBack();
                        }

                    }
                    else
                    {
                        monitor_progressbar.Visibility = Visibility.Collapsed;
                        MessageDialog md = new MessageDialog("Error occur update status. Try again later.");
                        await md.ShowAsync();

                    }
                }
                catch
                {
                    monitor_progressbar.Visibility = Visibility.Collapsed;
                    MessageDialog md = new MessageDialog("Error occur update status. Try again later.");
                    await md.ShowAsync();
                }
            }
        }


    }
}
