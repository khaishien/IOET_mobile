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
    public sealed partial class ElderlyPage_PositionLog : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();


        public ElderlyPage_PositionLog()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string elderlyid = e.Parameter as String;
            Debug.WriteLine("Here");
            LoadInDoorList(elderlyid);
            LoadOutDoorList(elderlyid);
        }

        private async void LoadOutDoorList(string elderlyid)
        {
            if (elderlyid != "")
            {
                outdoor_progressbar.Visibility = Visibility.Visible;
                object api = settings.Values["api"];
                object id = settings.Values["userid"];
                if (api != null && id != null)
                {
                    try
                    {
                        var httpClient = new HttpClient();
                        var response = await httpClient.GetAsync(common.getIP() + "api/outdoorlist/" + api.ToString() + "/" + id.ToString() + "/" + elderlyid);

                        if (response.IsSuccessStatusCode)
                        {
                            outdoor_progressbar.Visibility = Visibility.Collapsed;

                            string content = await response.Content.ReadAsStringAsync();
                            List<OutdoorModel> outdoorList = JsonConvert.DeserializeObject<List<OutdoorModel>>(content);

                            foreach (var model in outdoorList)
                            {
                                model.timestamp = model.timestamp.ToLocalTime();
                            }

                            outdoorList = outdoorList.OrderByDescending(d => d.timestamp).ToList();

                            OutdoorLvBinding.ItemsSource = outdoorList;

                        }
                        else
                        {
                            outdoor_progressbar.Visibility = Visibility.Collapsed;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        outdoor_progressbar.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    outdoor_progressbar.Visibility = Visibility.Collapsed;

                }
            }
        }

        private async void LoadInDoorList(string elderlyid)
        {
            if (elderlyid != "")
            {
                indoor_progressbar.Visibility = Visibility.Visible;
                object api = settings.Values["api"];
                object id = settings.Values["userid"];
                if (api != null && id != null)
                {
                    try
                    {
                        var httpClient = new HttpClient();
                        var response = await httpClient.GetAsync(common.getIP() + "api/indoorlist/" + api.ToString() + "/" + id.ToString() + "/" + elderlyid);

                        if (response.IsSuccessStatusCode)
                        {
                            indoor_progressbar.Visibility = Visibility.Collapsed;

                            string content = await response.Content.ReadAsStringAsync();
                            List<IndoorModel> indoorList = JsonConvert.DeserializeObject<List<IndoorModel>>(content);

                            foreach (var model in indoorList)
                            {
                                model.timestamp = model.timestamp.ToLocalTime();
                                //Debug.WriteLine(model.zoneid.locationname);
                                //Debug.WriteLine(model.timestamp);
                            }
                            indoorList = indoorList.OrderByDescending(d => d.timestamp).ToList();

                            IndoorLvBinding.ItemsSource = indoorList;

                        }
                        else
                        {
                            outdoor_progressbar.Visibility = Visibility.Collapsed;
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                        indoor_progressbar.Visibility = Visibility.Collapsed;
                    }


                }
            }
            else
            {
                indoor_progressbar.Visibility = Visibility.Collapsed;

            }
        }
        
        private void OutdoorLvBinding_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("Selected: {0}", e.AddedItems[0]);

            OutdoorModel outdoorModel = (OutdoorModel)e.AddedItems[0];
            if (outdoorModel != null)
            {
                Debug.WriteLine(outdoorModel.longitude + "," + outdoorModel.latitude);
                this.Frame.Navigate(typeof(ElderlyOutdoorLocationMap), outdoorModel);
            }
        }
    }
}
