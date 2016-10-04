using CaregiverMobile.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CaregiverMobile.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ElderlyPage : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();
        String tracklist_id;
        string ElderlyId;

        string GoogleMapKey = "AIzaSyDgH-OlY9aEGpTnTHYXGhqo3GhfkJoqaBE";
        string lastestOutdoorLat = "";
        string lastestOutdoorLng = "";

        private const string _GOOGLE_AUTOCOMPLETE_URL = "https://maps.googleapis.com/maps/api/place/details/json?";

        public ElderlyPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            tracklist_id = e.Parameter as String;
            LoadElderly(tracklist_id);
        }

        private async void LoadElderly(String tracklist_id)
        {

            load_elderly_progressbar.Visibility = Visibility.Visible;

            object api = settings.Values["api"];
            object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/tracklist/" + api.ToString() + "/" + id.ToString() + "/" + tracklist_id);
                    load_elderly_progressbar.Visibility = Visibility.Collapsed;
                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        TrackListModel trackListModel = JsonConvert.DeserializeObject<TrackListModel>(content);

                        Title.Text = trackListModel.elderlyid.userfullname;
                        ElderlyName.Text = trackListModel.elderlyid.userfullname;

                        LoadLocationList(trackListModel.elderlyid._id);
                        ElderlyId = trackListModel.elderlyid._id;

                        int c = 0;

                        if (trackListModel.outdoor == null)
                        {
                            if (trackListModel.indoor != null)
                            {
                                c = 1;
                            }
                            else
                            {
                                c = 0;
                            }
                        }
                        else
                        {
                            if (trackListModel.indoor != null)
                            {
                                c = 3;
                            }
                            else
                            {
                                c = 2;
                            }
                        }

                        
                        switch (c)
                        {
                            case 0:
                                ElderlyCoverage.Text = "-";
                                ElderlyLocationBtn.IsEnabled = false;

                                break;
                            case 1:

                                ElderlyCoverage.Text = "INDOOR";

                                ElderlyLocation.Text = trackListModel.indoor.zoneid.locationname;
                                LatestLocationTime.Text = trackListModel.indoor.timestamp.ToLocalTime().ToString();
                                ElderlyLocationBtn.IsEnabled = false;

                                break;
                            case 2:

                                lastestOutdoorLat = trackListModel.outdoor.latitude;
                                lastestOutdoorLng = trackListModel.outdoor.longitude;
                                FindAddress(trackListModel.outdoor.latitude, trackListModel.outdoor.longitude);
                                ElderlyCoverage.Text = "OUTDOOR";
                                LatestLocationTime.Text = trackListModel.outdoor.timestamp.ToLocalTime().ToString();
                                ElderlyLocationBtn.IsEnabled = true;

                                break;
                            case 3:
                                
                                if (trackListModel.indoor.timestamp > trackListModel.outdoor.timestamp)
                                {
                                    ElderlyCoverage.Text = "INDOOR";

                                    ElderlyLocation.Text = trackListModel.indoor.zoneid.locationname;
                                    LatestLocationTime.Text = trackListModel.indoor.timestamp.ToLocalTime().ToString();
                                    ElderlyLocationBtn.IsEnabled = false;

                                }
                                else
                                {
                                    lastestOutdoorLat = trackListModel.outdoor.latitude;
                                    lastestOutdoorLng = trackListModel.outdoor.longitude;
                                    FindAddress(trackListModel.outdoor.latitude, trackListModel.outdoor.longitude);
                                    ElderlyCoverage.Text = "OUTDOOR";
                                    LatestLocationTime.Text = trackListModel.outdoor.timestamp.ToLocalTime().ToString();
                                    ElderlyLocationBtn.IsEnabled = true;
                                }

                                break;
                        }


                        if (trackListModel.elderlyid.alert == true)
                        {

                            AlertStatus.Visibility = Visibility.Visible;

                            MessageDialog md = new MessageDialog("You want cancel elderly alert status?");

                            md.Commands.Add(new UICommand { Label = "Yes", Id = 0 });
                            md.Commands.Add(new UICommand { Label = "No", Id = 1 });
                            var res = await md.ShowAsync();

                            if ((int)res.Id == 0)
                            {
                                DisableAlertElderly(trackListModel.elderlyid._id);
                            }

                        }
                        else
                        {
                            AlertStatus.Visibility = Visibility.Collapsed;
                        }



                    }
                    else
                    {
                        showDialog();
                    }
                }
                catch (Exception ex)
                {
                    load_elderly_progressbar.Visibility = Visibility.Collapsed;

                    Debug.WriteLine(ex);
                    showDialog();
                }
            }
            else
            {
                load_elderly_progressbar.Visibility = Visibility.Collapsed;

                MessageDialog md = new MessageDialog("Api error occur. Relogin application.");
                await md.ShowAsync();

                settings.Values["login_status"] = false;
                Application.Current.Exit();
            }

        }

        private async void DisableAlertElderly(string _id)
        {
            if (_id != "")
            {
                object api = settings.Values["api"];
                object id = settings.Values["userid"];
                if (api != null && id != null)
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/caregiver/disablealert/" + api.ToString() + "/" + id.ToString() + "/" + _id);
                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("Disable alert success");
                        AlertStatus.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Debug.WriteLine("Disable alert failed");
                    }
                }

            }
        }

        private async void FindAddress(string latitude, string longitude)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("https://maps.googleapis.com/maps/api/geocode/json?latlng=" + latitude + "," + longitude + "&key=" + GoogleMapKey);

                if (response.IsSuccessStatusCode)
                {

                    string content = await response.Content.ReadAsStringAsync();
                    GoogleMapGeoLocationModel googleMapGeoLocationModel = JsonConvert.DeserializeObject<GoogleMapGeoLocationModel>(content);

                    ElderlyLocation.Text = googleMapGeoLocationModel.results[0].formatted_address;

                }
                else
                {
                    MessageDialog md = new MessageDialog("Error retrieve address from coordinate code.");
                    await md.ShowAsync();
                }
            }
            catch
            {
                MessageDialog md = new MessageDialog("Error retrieve address from coordinate code.");
                await md.ShowAsync();
            }
        }

        private async void LoadLocationList(string elderlyId)
        {
            progressbar.Visibility = Visibility.Visible;

            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/featuredlocation/" + api.ToString() + "/" + id.ToString() + "/" + elderlyId);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        List<OutdoorModel> outdoormodels = JsonConvert.DeserializeObject<List<OutdoorModel>>(content);

                        List<GooglePlaceIdModel> groupPlaces = new List<GooglePlaceIdModel>();

                        foreach (var a in outdoormodels)
                        {
                            string place_id = a.placeid;

                            if (groupPlaces.Count == 0)
                            {
                                List<OutdoorModel> l = new List<OutdoorModel>();
                                l.Add(a);

                                GooglePlaceIdModel newModel = new GooglePlaceIdModel()
                                {
                                    placeid = a.placeid,
                                    placeaddress = a.placeaddress,
                                    outdoorModels = l
                                };

                                groupPlaces.Add(newModel);
                            }
                            else
                            {
                                bool added = false;

                                foreach (var b in groupPlaces)
                                {

                                    if (b.placeid == place_id)
                                    {
                                        b.outdoorModels.Add(a);
                                        added = true;
                                        break;
                                    }


                                }
                                if (!added)
                                {
                                    List<OutdoorModel> l = new List<OutdoorModel>();
                                    l.Add(a);
                                    GooglePlaceIdModel newModel = new GooglePlaceIdModel()
                                    {
                                        placeid = a.placeid,
                                        placeaddress = a.placeaddress,
                                        outdoorModels = l
                                    };

                                    groupPlaces.Add(newModel);

                                }


                            }
                        }



                        int totalvisit = 0;

                        foreach (var z in groupPlaces)
                        {
                            z.outdoorModels = z.outdoorModels.OrderBy(d => d.timestamp).ToList();

                            int visit = 1;

                            int num = 0;

                            while (num < z.outdoorModels.Count)
                            {


                                DateTime previousDT = z.outdoorModels[num].timestamp;
                                DateTime thisDT;

                                if (num + 1 == z.outdoorModels.Count)
                                    thisDT = z.outdoorModels[num].timestamp;
                                else
                                    thisDT = z.outdoorModels[num + 1].timestamp;


                                TimeSpan duration = thisDT - previousDT;

                                if (duration.TotalMinutes > 20)
                                {
                                    visit++;
                                }
                                num++;

                            }

                            z.visit_count = visit;
                            totalvisit = totalvisit + visit;


                        }
                        double mean = (double)totalvisit / groupPlaces.Count;

                        List<GooglePlaceIdModel> newGroupPlaces = new List<GooglePlaceIdModel>();

                        //filter by mean
                        foreach (var o in groupPlaces)
                        {
                            if (o.visit_count >= mean)
                            //if (o.visit_count >= 0)
                                newGroupPlaces.Add(o);
                        }
                        newGroupPlaces = newGroupPlaces.OrderByDescending(d => d.visit_count).ToList();


                        FeatureLocationList.ItemsSource = newGroupPlaces;
                        progressbar.Visibility = Visibility.Collapsed;
                    }
                }
                catch
                {

                }
            }

        }

        private async Task<GoogleMapPlaceModel> ConvertPlaceIdToAddress(GooglePlaceIdModel m)
        {
            progressbar.Visibility = Visibility.Visible;

            try
            {

                HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync(_GOOGLE_AUTOCOMPLETE_URL + "placeid=" + m.placeid + "&key=" + GoogleMapKey);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    GoogleMapPlaceModel model = JsonConvert.DeserializeObject<GoogleMapPlaceModel>(content);
                    //model.count = m.count;
                    return model;

                    //SearchResultPlaceList.ItemsSource = foundPlaceModel.predictions;

                }
                else
                {
                    progressbar.Visibility = Visibility.Collapsed;
                    MessageDialog md = new MessageDialog("Error occur search place...try again later...");
                    await md.ShowAsync();
                    return null;
                }
            }
            catch
            {
                progressbar.Visibility = Visibility.Collapsed;
                MessageDialog md = new MessageDialog("Error occur search place...try again later...");
                await md.ShowAsync();
                return null;
            }
        }

        private async void FeatureLocationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            GooglePlaceIdModel model = (GooglePlaceIdModel)e.AddedItems[0];
            if (model != null)
            {
                GoogleMapPlaceModel returnModel = await ConvertPlaceIdToAddress(model);

                //Debug.WriteLine(model.result.geometry.location.lat);
                OutdoorModel m = new OutdoorModel()
                {
                    longitude = returnModel.result.geometry.location.lng.ToString(),
                    latitude = returnModel.result.geometry.location.lat.ToString()

                };
                Debug.WriteLine(m.longitude);
                Debug.WriteLine(m.latitude);

                this.Frame.Navigate(typeof(ElderlyOutdoorLocationMap), m);

            }



        }

        private async void showDialog()
        {
            MessageDialog md = new MessageDialog("Error occur loading elderly. Try again later.");

            md.Commands.Add(new UICommand { Label = "Try again", Id = 0 });
            md.Commands.Add(new UICommand { Label = "Close", Id = 1 });
            var res = await md.ShowAsync();

            if ((int)res.Id == 0)
            {
                LoadElderly(tracklist_id);
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

        private void menuBtn_Click(object sender, RoutedEventArgs e)
        {
            splitview.IsPaneOpen = !splitview.IsPaneOpen;
        }

        private void homeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private void profileBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ProfilePage));
        }

        private async void ElderlyLocation_Click(object sender, RoutedEventArgs e)
        {
            // Center on New York City
            //var uriNewYork = new Uri(@"bingmaps:?cp=40.726966~-74.006076&lvl=10");
            if (lastestOutdoorLat != "" && lastestOutdoorLng != "")
            {
                string uri = @"ms-drive-to:?destination.latitude=" + lastestOutdoorLat + "&destination.longitude=" + lastestOutdoorLng + "&destination.name=Elderly Position";
                var uriNavigateHome = new Uri(uri);
                //var uriNavigateHome = new Uri(@"ms-drive-to:?destination.latitude=47.680504&destination.longitude=-122.328262&destination.name=Green Lake");
                // Launch the Windows Maps app
                var launcherOptions = new Windows.System.LauncherOptions();
                launcherOptions.TargetApplicationPackageFamilyName = "Microsoft.WindowsMaps_8wekyb3d8bbwe";
                await Windows.System.Launcher.LaunchUriAsync(uriNavigateHome, launcherOptions);
            }

        }

        private void NotificationBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(NotificationPage));
        }

        private async void SyncBtn_Click(object sender, RoutedEventArgs e)
        {
            //train data
            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/featuredlocation/train/" + api.ToString() + "/" + id.ToString() + "/" + ElderlyId);

                    if (response.IsSuccessStatusCode)
                    {

                        MessageDialog md = new MessageDialog("Train request sent.");
                        await md.ShowAsync();
                    }
                    else
                    {
                        //monitor_progressbar.Visibility = Visibility.Collapsed;
                        MessageDialog md = new MessageDialog("Error occur when send train featured location request. Try again later.");
                        await md.ShowAsync();
                    }


                }
                catch
                {
                    MessageDialog md = new MessageDialog("Error occur when send train featured location request. Try again later.");
                    await md.ShowAsync();
                }
            }

        }

        private void HeatMapBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(HeatMapPage), ElderlyId);
        }

        private void LocationLogBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ElderlyPage_PositionLog), ElderlyId);
        }
    }
}
