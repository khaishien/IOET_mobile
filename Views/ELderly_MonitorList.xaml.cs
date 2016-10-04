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
    public sealed partial class ELderly_MonitorList : Page
    {

        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();
        List<TrackListModel> tracklistModel = null;

        public ELderly_MonitorList()
        {
            this.InitializeComponent();
            LoadMonitorList();
        }

        private async void LoadMonitorList()
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];

            if (api != null && id != null)
            {
                monitor_progressbar.Visibility = Visibility.Visible;
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/monitorlist/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        tracklistModel = JsonConvert.DeserializeObject<List<TrackListModel>>(content);

                        CaregiverlvBinding.ItemsSource = tracklistModel;
                        monitor_progressbar.Visibility = Visibility.Collapsed;

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
                    monitor_progressbar.Visibility = Visibility.Collapsed;
                    MessageDialog md = new MessageDialog("Error occur loading monitor list. Try again later.");
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

        private async void CaregiverlvBinding_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //disable monitor progress
            TrackListModel model = (TrackListModel)e.AddedItems[0];
            if (model != null)
            {
                MessageDialog md = new MessageDialog("Are you sure to cancel monitor by caregiver " + model.caregiverid.userfullname);

                md.Commands.Add(new UICommand { Label = "Yes", Id = 0 });
                md.Commands.Add(new UICommand { Label = "No", Id = 1 });
                var res = await md.ShowAsync();

                if ((int)res.Id == 0)
                {
                    cancelMonitor(model._id);
                }
            }
        }

        private async void cancelMonitor(string _id)
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];

            if (api != null && id != null)
            {
                monitor_progressbar.Visibility = Visibility.Visible;

                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/cancelmonitor/" + api.ToString() + "/" + id.ToString() + "/" + _id);

                    if (response.IsSuccessStatusCode)
                    {
                        monitor_progressbar.Visibility = Visibility.Collapsed;
                        MessageDialog md = new MessageDialog("Cancel monitor successful.");
                        await md.ShowAsync();

                        if (Frame.CanGoBack)
                        {
                            Frame.GoBack();
                        }
                    }
                    else
                    {
                        monitor_progressbar.Visibility = Visibility.Collapsed;
                        MessageDialog md = new MessageDialog("Error occur cancel monitor. Try again later.");
                        await md.ShowAsync();
                    }
                }
                catch
                {
                    monitor_progressbar.Visibility = Visibility.Collapsed;
                    MessageDialog md = new MessageDialog("Error occur cancel monitor. Try again later.");
                    await md.ShowAsync();
                }
            }
        }

        private void BackBtn_Click_1(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}
