using CaregiverMobile.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
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
    public sealed partial class Elderly_RequestList : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();
        List<RequestModel> requestModel = null;

        ObservableCollection<RequestModel> items = new ObservableCollection<RequestModel>();
        
        public Elderly_RequestList()
        {
            this.InitializeComponent();
            LoadRequestList();
        }

        private async void LoadRequestList()
        {

            object api = settings.Values["api"];
            object id = settings.Values["userid"];

            if (api != null && id != null)
            {
                monitor_progressbar.Visibility = Visibility.Visible;
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/requestlist/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        requestModel = JsonConvert.DeserializeObject<List<RequestModel>>(content);

                        foreach (var model in requestModel)
                        {
                            items.Add(model);
                            Debug.WriteLine("initial: "+model._id+","+model.requeststatus);
                        }

                        DataContext = items;
                        monitor_progressbar.Visibility = Visibility.Collapsed;

                    }
                    else
                    {
                        monitor_progressbar.Visibility = Visibility.Collapsed;
                        MessageDialog md = new MessageDialog("Error occur loading request list. Try again later.");
                        await md.ShowAsync();

                    }
                }
                catch
                {
                    monitor_progressbar.Visibility = Visibility.Collapsed;
                    MessageDialog md = new MessageDialog("Error occur loading request list. Try again later.");
                    await md.ShowAsync();
                }
            }
        }

        private async void MySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RequestModel requestModel = (RequestModel)e.AddedItems[0];
            if (requestModel != null)
            {
                if (requestModel.requeststatus == false)
                {
                    MessageDialog md = new MessageDialog("Are you sure to accept request of caregiver " + requestModel.caregiverid.userfullname);

                    md.Commands.Add(new UICommand { Label = "Yes", Id = 0 });
                    md.Commands.Add(new UICommand { Label = "No", Id = 1 });
                    var res = await md.ShowAsync();

                    if ((int)res.Id == 0)
                    {
                        UpdateTrackStatus(requestModel._id, true);
                    }
                }
                //else
                //{
                //    MessageDialog md = new MessageDialog("Are you sure to cancel monitor of caregiver " + requestModel.caregiverid.userfullname);

                //    md.Commands.Add(new UICommand { Label = "Yes", Id = 0 });
                //    md.Commands.Add(new UICommand { Label = "No", Id = 1 });
                //    var res = await md.ShowAsync();

                //    if ((int)res.Id == 0)
                //    {
                //        UpdateTrackStatus(requestModel._id, false);
                //    }
                //}

            }
        }

        private async void UpdateTrackStatus(string _id, bool v)
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];

            if (api != null && id != null)
            {
                monitor_progressbar.Visibility = Visibility.Visible;
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/updaterequest/" + api.ToString() + "/" + id.ToString() + "/" + _id + "/" + v);


                    if (response.IsSuccessStatusCode)
                    {
                        //string content = await response.Content.ReadAsStringAsync();
                        monitor_progressbar.Visibility = Visibility.Collapsed;
                        MessageDialog md = new MessageDialog("Updated status successful.");
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

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

    }
}
