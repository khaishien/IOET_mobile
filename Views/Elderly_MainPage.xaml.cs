using CaregiverMobile.Models;
using NdefLibrary.Ndef;
using NdefLibraryUwp.Ndef;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Windows.Foundation;
using Windows.Networking.Proximity;
using Windows.Networking.PushNotifications;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CaregiverMobile.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Elderly_MainPage : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();
        UserModel userModel;
        Geolocator geolocator = null;
        LocationDataModel lastestLocation = new LocationDataModel();

        private const string BackgroundTaskName = "SampleLocationBackgroundTask";
        private const string BackgroundTaskEntryPoint = "BackgroundTask.LocationBackgroundTask";

        private const string GeoBackgroundTaskName = "GeoLocationBackgroundTask";
        private const string GeoBackgroundTaskEntryPoint = "GeoBackgroundTask.LocationBackgroundTask";

        private IBackgroundTaskRegistration _geolocTask = null;

        PushNotificationChannel channel = null;


        private ProximityDevice _device;
        private long _subscriptionIdNdef;
        private readonly CoreDispatcher _dispatcher;
        private IList<Geofence> geofences;

        string fenceId = "home";
        BasicGeoposition position;
        double radius = 50; // in meters



        public Elderly_MainPage()
        {
            this.InitializeComponent();

            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                return;
            }
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            UpdateUiForNfcStatusAsync();
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
            LoadProfile();
            UpdateChannelUri();
            updateLastLogin();
            GetHomeLocationBuildGeoFence();
            //}
        }

        private async void GetHomeLocationBuildGeoFence()
        {

            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/homelocation/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        OutdoorModel model = JsonConvert.DeserializeObject<OutdoorModel>(content);


                        //load geofence
                        position.Latitude = Convert.ToDouble(model.latitude);
                        position.Longitude = Convert.ToDouble(model.longitude);
                        position.Altitude = 0.0;
                        LoggingMsg("Home location : lat- " + model.latitude + ", lng- " + model.longitude);

                        // Set a circular region for the geofence.
                        Geocircle geocircle = new Geocircle(position, radius);

                        // Remove the geofence after the first trigger.
                        bool singleUse = false;

                        // Set the monitored states.
                        MonitoredGeofenceStates monitoredStates =
                                        MonitoredGeofenceStates.Entered |
                                        MonitoredGeofenceStates.Exited;

                        // Set how long you need to be in geofence for the enter event to fire.
                        TimeSpan dwellTime = TimeSpan.FromSeconds(15);

                        // Set how long the geofence should be active.
                        TimeSpan duration = TimeSpan.FromDays(1);

                        // Set up the start time of the geofence.
                        DateTimeOffset startTime = DateTime.Now;

                        // Create the geofence.
                        Geofence geofence = new Geofence(fenceId, geocircle, monitoredStates, singleUse, dwellTime, startTime, duration);
                        try
                        {
                            GeofenceMonitor.Current.Geofences.Add(geofence);
                        }
                        catch
                        {
                            Debug.WriteLine("GeoFence Added same id");
                        }
                    }

                }
                catch
                {

                }
            }


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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            if (e.Parameter is Uri)
            {
                var uri = e.Parameter as Uri;
                //Debug.WriteLine(uri);
                //Debug.WriteLine(uri.Query);

                var lat = uri.ParseQueryString().Get("lat");
                var lng = uri.ParseQueryString().Get("lng");

                UpdateLocationData(lat, lng);
                NotifyCaregiver();

            }


            // Loop through all background tasks to see if SampleBackgroundTaskName is already registered
            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name == BackgroundTaskName)
                {
                    _geolocTask = cur.Value;
                    break;
                }
            }

            if (_geolocTask != null)
            {
                // Associate an event handler with the existing background task
                _geolocTask.Completed += OnCompleted;

                try
                {
                    BackgroundAccessStatus backgroundAccessStatus = BackgroundExecutionManager.GetAccessStatus();

                    switch (backgroundAccessStatus)
                    {
                        case BackgroundAccessStatus.Unspecified:
                        case BackgroundAccessStatus.Denied:
                            LoggingMsg("Not able to run in background. Application must be added to the lock screen.");
                            break;

                        default:
                            LoggingMsg("Background task is already registered. Waiting for next update...");

                            accessCurrentLocation();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LoggingMsg(ex.ToString());
                }
                UpdateButtonStates(/*registered:*/ true);
            }
            else
            {
                UpdateButtonStates(/*registered:*/ false);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_geolocTask != null)
            {
                // Remove the event handler
                _geolocTask.Completed -= OnCompleted;
                //GeofenceMonitor.Current.GeofenceStateChanged -= OnGeofenceStateChanged;
                //GeofenceMonitor.Current.StatusChanged -= OnGeofenceStatusChanged;
            }


            base.OnNavigatingFrom(e);
        }

        private void OnPushNotification(PushNotificationChannel sender, PushNotificationReceivedEventArgs e)
        {
            String notificationContent = String.Empty;

            switch (e.NotificationType)
            {
                case PushNotificationType.Badge:
                    notificationContent = e.BadgeNotification.Content.GetXml();
                    LoggingMsg(notificationContent);
                    break;

                case PushNotificationType.Tile:
                    notificationContent = e.TileNotification.Content.GetXml();
                    LoggingMsg(notificationContent);

                    break;

                case PushNotificationType.Toast:
                    notificationContent = e.ToastNotification.Content.GetXml();
                    LoggingMsg(notificationContent);

                    break;

                case PushNotificationType.Raw:
                    notificationContent = e.RawNotification.Content;
                    LoggingMsg(notificationContent);
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
                    channel.PushNotificationReceived += OnPushNotification;

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

        private async void LoadProfile()
        {
            UpdateButtonVisibily(false);
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
                        userModel = JsonConvert.DeserializeObject<UserModel>(content);
                        setEnableBtn(true);

                        ElderlyName.Text = userModel.userfullname;
                        if (userModel.userprofilepic == null)
                            ElderlyProfilePic.UriSource = new Uri(common.getIP() + "images/profile/null");
                        else
                            ElderlyProfilePic.UriSource = new Uri(common.getIP() + "images/profile/" + userModel.userprofilepic);

                        //do background process GPS........
                        //accessCurrentLocation();

                        UpdateButtonVisibily(true);
                    }
                    else
                    {
                        UpdateButtonVisibily(false);
                        setEnableBtn(false);
                        showDialog();
                    }
                }
                catch
                {
                    UpdateButtonVisibily(false);
                    setEnableBtn(false);
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

        private async void RegisterBackgroundTask()
        {
            try
            {
                // Get permission for a background task from the user. If the user has already answered once,
                // this does nothing and the user must manually update their preference via PC Settings.
                BackgroundAccessStatus backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();

                // Regardless of the answer, register the background task. If the user later adds this application
                // to the lock screen, the background task will be ready to run.
                // Create a new background task builder
                BackgroundTaskBuilder geolocTaskBuilder = new BackgroundTaskBuilder();

                geolocTaskBuilder.Name = BackgroundTaskName;
                geolocTaskBuilder.TaskEntryPoint = BackgroundTaskEntryPoint;

                TimeTrigger hourlyTrigger = new TimeTrigger(15, false);
                var trigger = new LocationTrigger(LocationTriggerType.Geofence);

                geolocTaskBuilder.SetTrigger(hourlyTrigger);
                geolocTaskBuilder.SetTrigger(trigger);

                //SystemTrigger internetTrigger = new SystemTrigger(SystemTriggerType.InternetAvailable, false);
                //geolocTaskBuilder.SetTrigger(internetTrigger);

                SystemCondition internetCondition = new SystemCondition(SystemConditionType.InternetAvailable);
                geolocTaskBuilder.AddCondition(internetCondition);

                // Register the background task
                _geolocTask = geolocTaskBuilder.Register();

                // Associate an event handler with the new background task
                _geolocTask.Completed += OnCompleted;

                UpdateButtonStates(/*registered*/ true);

                switch (backgroundAccessStatus)
                {
                    case BackgroundAccessStatus.Unspecified:
                    case BackgroundAccessStatus.Denied:
                        LoggingMsg("Not able to run in background. Application must be added to the lock screen.");
                        break;

                    default:
                        // BckgroundTask is allowed
                        LoggingMsg("Background task registered.");
                        // Need to request access to location
                        // This must be done with the background task registeration
                        // because the background task cannot display UI.
                        accessCurrentLocation();
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggingMsg(ex.ToString());
                UpdateButtonStates(/*registered:*/ false);
            }
        }

        private void UnregisterBackgroundTask()
        {
            // Unregister the background task
            if (null != _geolocTask)
            {
                _geolocTask.Unregister(true);
                _geolocTask = null;
            }

            //ScenarioOutput_Latitude.Text = "No data";
            //ScenarioOutput_Longitude.Text = "No data";
            //ScenarioOutput_Accuracy.Text = "No data";
            UpdateButtonStates(/*registered:*/ false);
            LoggingMsg("Background task unregistered");
        }

        private async void accessCurrentLocation()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();

            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:
                    LoggingMsg("Waiting for update...");
                    geofences = GeofenceMonitor.Current.Geofences;

                    foreach (var a in geofences)
                    {
                        Debug.WriteLine(a.Id);
                        //GeofenceMonitor.Current.Geofences.Remove(a);
                    }

                    // Register for state change events.
                    GeofenceMonitor.Current.GeofenceStateChanged += OnGeofenceStateChanged;
                    GeofenceMonitor.Current.StatusChanged += OnGeofenceStatusChanged;

                    geolocator = new Geolocator { ReportInterval = 30000 };

                    // Subscribe to the PositionChanged event to get location updates.
                    geolocator.PositionChanged += OnPositionChanged;

                    // Subscribe to the StatusChanged event to get updates of location status changes.
                    geolocator.StatusChanged += OnStatusChanged;

                    break;

                case GeolocationAccessStatus.Denied:
                    LoggingMsg("Access to location is denied.");

                    UpdateButtonStates(false);

                    LocationDisabledMessage.Visibility = Visibility.Visible;
                    UpdateLocationData(null);
                    break;

                case GeolocationAccessStatus.Unspecified:
                    LoggingMsg("Unspecified error.");

                    UpdateButtonStates(false);

                    UpdateLocationData(null);
                    break;
            }
        }

        public async void OnGeofenceStateChanged(GeofenceMonitor sender, object e)
        {
            var reports = sender.ReadReports();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (GeofenceStateChangeReport report in reports)
                {
                    GeofenceState state = report.NewState;

                    Geofence geofence = report.Geofence;

                    //if (state == GeofenceState.Removed)
                    //{
                    //    // Remove the geofence from the geofences collection.
                    //    GeofenceMonitor.Current.Geofences.Remove(geofence);
                    //    Debug.WriteLine("geofence remove");
                    //}
                    //else 
                    if (state == GeofenceState.Entered)
                    {
                        // Your app takes action based on the entered event.

                        // NOTE: You might want to write your app to take a particular
                        // action based on whether the app has internet connectivity.

                        SyncServerInHome(true);
                        LoggingMsg("Geofencing home entered");
                        Debug.WriteLine("geofence in");
                    }
                    else if (state == GeofenceState.Exited)
                    {
                        // Your app takes action based on the exited event.

                        // NOTE: You might want to write your app to take a particular
                        // action based on whether the app has internet connectivity.
                        Debug.WriteLine("geofence out");
                        LoggingMsg("Geofencing home exited");

                        SyncServerInHome(false);

                    }
                }
            });
        }

        private async void SyncServerInHome(bool v)
        {

            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/promptcaregiver/" + api.ToString() + "/" + id.ToString() + "/" + v.ToString());

                    if (response.IsSuccessStatusCode)
                    {

                    }
                }
                catch
                {

                }
            }
        }

        public async void OnGeofenceStatusChanged(GeofenceMonitor sender, object e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Show the location setting message only if the status is disabled.
                LocationDisabledMessage.Visibility = Visibility.Collapsed;

                switch (sender.Status)
                {
                    case GeofenceMonitorStatus.Ready:
                        LoggingMsg("The monitor is ready and active.");
                        break;

                    case GeofenceMonitorStatus.Initializing:
                        LoggingMsg("The monitor is in the process of initializing.");
                        break;

                    case GeofenceMonitorStatus.NoData:
                        LoggingMsg("There is no data on the status of the monitor.");
                        break;

                    case GeofenceMonitorStatus.Disabled:
                        LoggingMsg("Access to location is denied.");

                        // Show the message to the user to go to the location settings.
                        LocationDisabledMessage.Visibility = Visibility.Visible;
                        break;

                    case GeofenceMonitorStatus.NotInitialized:
                        LoggingMsg("The geofence monitor has not been initialized.");
                        break;

                    case GeofenceMonitorStatus.NotAvailable:
                        LoggingMsg("The geofence monitor is not available.");
                        break;

                    default:
                        //ScenarioOutput_Status.Text = "Unknown";
                        LoggingMsg("geofence unknown error");
                        break;
                }
            });
        }

        private void UpdateLocationData(Geoposition pos)
        {
            if (pos != null)
            {
                double lng = pos.Coordinate.Point.Position.Longitude;
                double lat = pos.Coordinate.Point.Position.Latitude;

                lastestLocation.lng = lng.ToString();
                lastestLocation.lat = lat.ToString();

                //Debug.WriteLine(lastestLocation.lng+" "+lastestLocation.lat);
                LoggingMsg("Update location data: lat:" + lastestLocation.lat + " lng:" + lastestLocation.lng);


                sendLocationData(lastestLocation.lng, lastestLocation.lat);

            }

        }

        private void UpdateLocationData(string lat, string lng)
        {
            if (lat != null && lng != null)
            {

                lastestLocation.lng = lng;
                lastestLocation.lat = lat;

                //Debug.WriteLine(lastestLocation.lng+" "+lastestLocation.lat);
                LoggingMsg("Update location data: lat:" + lastestLocation.lat + " lng:" + lastestLocation.lng);


                sendLocationData(lastestLocation.lng, lastestLocation.lat);

            }

        }

        private async void sendLocationData(string lng, string lat)
        {

            LocationDataModel model = new LocationDataModel
            {
                lat = lat,
                lng = lng
            };

            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];

            if (api != null && id != null)
            {
                try
                {
                    Common common = new Common();
                    var httpClient = new HttpClient();
                    var response = await httpClient.PostAsJsonAsync(common.getIP() + "api/elderly/outdoor/" + api.ToString() + "/" + id.ToString(), model);

                    if (response.IsSuccessStatusCode)
                    {
                        LoggingMsg("Location sent successful.");
                    }
                    else
                    {
                        LoggingMsg("Error occur when send location data.");
                        //MessageDialog md = new MessageDialog("Error send data to server. Try again later.");
                        //await md.ShowAsync();
                    }

                }
                catch
                {
                    LoggingMsg("Error occur when send location data.");
                    //MessageDialog md = new MessageDialog("Error send data to server. Try again later.");
                    //await md.ShowAsync();
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

        private async void NotifyCaregiver()
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/updatenfc/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("sent by nfc.");
                    }
                    else
                    {
                        Debug.WriteLine("error by nfc.");
                    }
                }
                catch
                {
                    Debug.WriteLine("catch by nfc.");
                }
            }
        }

        async private void OnStatusChanged(Geolocator sender, StatusChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Show the location setting message only if status is disabled.
                LocationDisabledMessage.Visibility = Visibility.Collapsed;

                switch (e.Status)
                {
                    case PositionStatus.Ready:
                        // Location platform is providing valid data.
                        Status.Text = "Ready";
                        Status.Foreground = new SolidColorBrush(Colors.Green);
                        LoggingMsg("Location platform is ready.");
                        break;

                    case PositionStatus.Initializing:
                        // Location platform is attempting to acquire a fix. 
                        Status.Text = "Initializing";
                        Status.Foreground = new SolidColorBrush(Colors.Green);
                        LoggingMsg("Location platform is attempting to obtain a position.");
                        break;

                    case PositionStatus.NoData:
                        // Location platform could not obtain location data.
                        Status.Text = "No data";
                        Status.Foreground = new SolidColorBrush(Colors.Red);
                        LoggingMsg("Not able to determine the location.");
                        break;

                    case PositionStatus.Disabled:
                        // The permission to access location data is denied by the user or other policies.
                        Status.Text = "Disabled";
                        Status.Foreground = new SolidColorBrush(Colors.Red);
                        LoggingMsg("Access to location is denied.");
                        // Show message to the user to go to location settings.
                        LocationDisabledMessage.Visibility = Visibility.Visible;

                        // Clear any cached location data.
                        UpdateLocationData(null);

                        //Notify Caregiver Location service closed
                        /////////////////
                        AlertLocationServiceOff();

                        break;

                    case PositionStatus.NotInitialized:
                        // The location platform is not initialized. This indicates that the application 
                        // has not made a request for location data.
                        Status.Text = "Not initialized";
                        Status.Foreground = new SolidColorBrush(Colors.Red);
                        LoggingMsg("No request for location is made yet.");
                        break;

                    case PositionStatus.NotAvailable:
                        // The location platform is not available on this version of the OS.
                        Status.Text = "Not available";
                        Status.Foreground = new SolidColorBrush(Colors.Red);
                        LoggingMsg("Location is not available on this version of the OS.");
                        break;

                    default:
                        Status.Text = "Unknown";
                        Status.Foreground = new SolidColorBrush(Colors.Red);
                        LoggingMsg(string.Empty);
                        break;
                }
            });
        }

        async private void OnPositionChanged(Geolocator sender, PositionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                LoggingMsg("Location position updating.");
                UpdateLocationData(e.Position);
            });
        }

        private async void OnCompleted(IBackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs e)
        {
            if (sender != null)
            {
                // Update the UI with progress reported by the background task
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    try
                    {
                        // If the background task threw an exception, display the exception in
                        // the error text box.
                        e.CheckResult();
                        LoggingMsg("Refresh background service.");
                    }
                    catch (Exception ex)
                    {
                        // The background task had an error
                        LoggingMsg(ex.ToString());
                    }
                });
            }
        }

        private void setEnableBtn(Boolean mode)
        {
            NavigateHomeBtn.IsEnabled = mode;
            NotificationBtn.IsEnabled = mode;
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
        }

        private async void AlertLocationServiceOff()
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/alert/" + api.ToString() + "/" + id.ToString() + "/2");

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("sent.");
                    }
                    else
                    {
                        Debug.WriteLine("error.");
                    }
                }
                catch
                {
                    Debug.WriteLine("catch.");
                }
            }
        }

        private void NavigateHomeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (userModel != null)
            {
                Navigate(userModel.userHomeLat, userModel.userHomeLng);
                NotifyCaregiverNavigate();
            }
        }

        private async void NotifyCaregiverNavigate()
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/promptcaregiverlocation/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("prompt caregiver location sent.");
                    }
                    else
                    {
                        Debug.WriteLine("prompt caregiver location error.");
                    }
                }
                catch
                {
                        Debug.WriteLine("prompt caregiver location error.");
                }

            }
        }

        private async void promptNFCtaped(string lat,string lng)
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/promptcaregivernfc/" + api.ToString() + "/" + id.ToString() + "/" + lat + "/" + lng);

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("prompt caregiver nfc sent.");
                    }
                    else
                    {
                        Debug.WriteLine("prompt caregiver nfc error.");
                    }
                }
                catch
                {
                    Debug.WriteLine("prompt caregiver nfc error.");
                }

            }
        }

        private void NotificationBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(NotificationPage));
        }

        private void SettingBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage), "elderly");
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadProfile();
        }

        private void LogBtn_Click(object sender, RoutedEventArgs e)
        {
            LogView.Visibility = Visibility.Visible;
        }

        private void CloseLogBtn_Click(object sender, RoutedEventArgs e)
        {
            LogView.Visibility = Visibility.Collapsed;
        }

        private async void Navigate(String lat, String lng)
        {
            // Center on New York City
            //var uriNewYork = new Uri(@"bingmaps:?cp=40.726966~-74.006076&lvl=10");
            string uri = @"ms-drive-to:?destination.latitude=" + lat + "&destination.longitude=" + lng + "&destination.name=Home";
            var uriNavigateHome = new Uri(uri);
            //var uriNavigateHome = new Uri(@"ms-drive-to:?destination.latitude=47.680504&destination.longitude=-122.328262&destination.name=Green Lake");
            // Launch the Windows Maps app
            var launcherOptions = new Windows.System.LauncherOptions();
            launcherOptions.TargetApplicationPackageFamilyName = "Microsoft.WindowsMaps_8wekyb3d8bbwe";
            var success = await Windows.System.Launcher.LaunchUriAsync(uriNavigateHome, launcherOptions);


        }

        private void StartTrackingBtn_Click(object sender, RoutedEventArgs e)
        {
            RegisterBackgroundTask();
        }

        private void StopTrackingBtn_Click(object sender, RoutedEventArgs e)
        {
            UnregisterBackgroundTask();
            stopTrack();
        }

        private void stopTrack()
        {
            geolocator.PositionChanged -= OnPositionChanged;
            geolocator.StatusChanged -= OnStatusChanged;
            geolocator = null;

            // Clear status
            LoggingMsg("Location Stop updating.");
        }

        private async void UpdateButtonStates(bool registered)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    StartTrackingBtn.IsEnabled = !registered;
                    StopTrackingBtn.IsEnabled = registered;
                });
        }

        private void UpdateButtonVisibily(bool on)
        {
            if (on)
            {
                StartTrackingBtn.Visibility = Visibility.Visible;
                StopTrackingBtn.Visibility = Visibility.Visible;
            }
            else
            {
                StartTrackingBtn.Visibility = Visibility.Collapsed;
                StopTrackingBtn.Visibility = Visibility.Collapsed;
            }

        }

        private async void LoggingMsg(string msg)
        {
            //StatusBlock.Text = StatusBlock.Text + System.Environment.NewLine + msg;
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StatusBlock.Text = StatusBlock.Text + Environment.NewLine + msg;
            });
        }


        private void InitNfcBtn_Click(object sender, RoutedEventArgs e)
        {
            _device = ProximityDevice.GetDefault();

            if (_device != null)
            {
                _device.DeviceArrived += NfcDeviceArrived;
                _device.DeviceDeparted += NfcDeviceDeparted;
            }

            LoggingMsg(_device != null ? "StatusInitialized" : "StatusInitFailed");

            UpdateUiForNfcStatusAsync();
        }

        private async void UpdateUiForNfcStatusAsync()
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                InitNfcBtn.IsEnabled = (_device == null);

                // Subscription buttons
                NfcBtn.IsEnabled = (_device != null && _subscriptionIdNdef == 0);
                NfcStopBtn.IsEnabled = (_device != null && _subscriptionIdNdef != 0);

            });
        }

        #region Device Arrived / Departed
        private void NfcDeviceDeparted(ProximityDevice sender)
        {
            LoggingMsg("DeviceDeparted");
        }

        private void NfcDeviceArrived(ProximityDevice sender)
        {
            LoggingMsg("DeviceArrived");
        }
        #endregion

        #region Subscribe for tags
        // ----------------------------------------------------------------------------------------------------
        private void NfcBtn_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            Debug.WriteLine(_subscriptionIdNdef);
            // Only subscribe for messages if no NDEF subscription is already active
            if (_subscriptionIdNdef != 0) return;
            // Ask the proximity device to inform us about any kind of NDEF message received from
            // another device or tag.
            // Store the subscription ID so that we can cancel it later.
            _subscriptionIdNdef = _device.SubscribeForMessage("NDEF", MessageReceivedHandler);
            // Update status text for UI
            LoggingMsg("StatusSubscribed " + _subscriptionIdNdef);
            // Update enabled / disabled state of buttons in the User Interface
            UpdateUiForNfcStatusAsync();
        }
        #endregion 

        private async void MessageReceivedHandler(ProximityDevice sender, ProximityMessage message)
        {
            // Get the raw NDEF message data as byte array
            var rawMsg = message.Data.ToArray();
            NdefMessage ndefMessage = null;
            try
            {
                // Let the NDEF library parse the NDEF message out of the raw byte array
                ndefMessage = NdefMessage.FromByteArray(rawMsg);
            }
            catch (NdefException e)
            {
                LoggingMsg("InvalidNdef" + e.Message);
                return;
            }

            // Analysis result
            var tagContents = new StringBuilder();

            // Parse tag contents
            try
            {
                // Parse the contents of the tag
                await ParseTagContents(ndefMessage, tagContents);

                // Update status text for UI
                LoggingMsg("StatusTagParsed" + tagContents);
            }
            catch (Exception ex)
            {
                LoggingMsg("StatusNfcParsingError" + ex.Message);
            }

        }

        private async Task ParseTagContents(NdefMessage ndefMessage, StringBuilder tagContents)
        {
            // Loop over all records contained in the NDEF message
            foreach (NdefRecord record in ndefMessage)
            {
                // --------------------------------------------------------------------------
                // Print generic information about the record
                if (record.Id != null && record.Id.Length > 0)
                {
                    // Record ID (if present)
                    tagContents.AppendFormat("Id: {0}\n", Encoding.UTF8.GetString(record.Id, 0, record.Id.Length));
                }
                // Record type name, as human readable string
                tagContents.AppendFormat("Type name: {0}\n", ConvertTypeNameFormatToString(record.TypeNameFormat));
                // Record type
                if (record.Type != null && record.Type.Length > 0)
                {
                    tagContents.AppendFormat("Record type: {0}\n",
                        Encoding.UTF8.GetString(record.Type, 0, record.Type.Length));
                }

                // --------------------------------------------------------------------------
                // Check the type of each record
                // Using 'true' as parameter for CheckSpecializedType() also checks for sub-types of records,
                // e.g., it will return the SMS record type if a URI record starts with "sms:"
                // If using 'false', a URI record will always be returned as Uri record and its contents won't be further analyzed
                // Currently recognized sub-types are: SMS, Mailto, Tel, Nokia Accessories, NearSpeak, WpSettings
                var specializedType = record.CheckSpecializedType(true);

                if (specializedType == typeof(NdefMailtoRecord))
                {
                    // --------------------------------------------------------------------------
                    // Convert and extract Mailto record info
                    var mailtoRecord = new NdefMailtoRecord(record);
                    tagContents.Append("-> Mailto record\n");
                    tagContents.AppendFormat("Address: {0}\n", mailtoRecord.Address);
                    tagContents.AppendFormat("Subject: {0}\n", mailtoRecord.Subject);
                    tagContents.AppendFormat("Body: {0}\n", mailtoRecord.Body);
                }
                else if (specializedType == typeof(NdefUriRecord))
                {
                    // --------------------------------------------------------------------------
                    // Convert and extract URI record info
                    var uriRecord = new NdefUriRecord(record);
                    tagContents.Append("-> URI record\n");
                    tagContents.AppendFormat("URI: {0}\n", uriRecord.Uri);

                    checkGeoUri(uriRecord.Uri);

                }
                else if (specializedType == typeof(NdefSpRecord))
                {
                    // --------------------------------------------------------------------------
                    // Convert and extract Smart Poster info
                    var spRecord = new NdefSpRecord(record);
                    tagContents.Append("-> Smart Poster record\n");
                    tagContents.AppendFormat("URI: {0}", spRecord.Uri);
                    tagContents.AppendFormat("Titles: {0}", spRecord.TitleCount());
                    if (spRecord.TitleCount() > 1)
                        tagContents.AppendFormat("1. Title: {0}", spRecord.Titles[0].Text);
                    tagContents.AppendFormat("Action set: {0}", spRecord.ActionInUse());
                    // You can also check the action (if in use by the record), 
                    // mime type and size of the linked content.
                }
                else if (specializedType == typeof(NdefLaunchAppRecord))
                {
                    // --------------------------------------------------------------------------
                    // Convert and extract LaunchApp record info
                    var launchAppRecord = new NdefLaunchAppRecord(record);
                    tagContents.Append("-> LaunchApp record" + Environment.NewLine);
                    if (!string.IsNullOrEmpty(launchAppRecord.Arguments))
                        tagContents.AppendFormat("Arguments: {0}\n", launchAppRecord.Arguments);
                    if (launchAppRecord.PlatformIds != null)
                    {
                        foreach (var platformIdTuple in launchAppRecord.PlatformIds)
                        {
                            if (platformIdTuple.Key != null)
                                tagContents.AppendFormat("Platform: {0}\n", platformIdTuple.Key);
                            if (platformIdTuple.Value != null)
                                tagContents.AppendFormat("App ID: {0}\n", platformIdTuple.Value);
                        }
                    }
                }
                else
                {
                    // Other type, not handled by this demo
                    tagContents.Append("NDEF record not parsed by this demo app" + Environment.NewLine);
                }
            }
        }

        private void checkGeoUri(string uri)
        {

            string[] words = uri.Split(':');

            if (words.Length > 0)
            {
                if (words[0] == "geo" || words[0] == "GEO")
                {
                    LoggingMsg("Uri contain GEO type data. Processing...");

                    var temp = words[1];

                    words = temp.Split(',');

                    var lat = words[0];
                    var lng = words[1];

                    //LoggingMsg("Sending lat:" + lat + ", lng:" + lng + " to server.");

                    UpdateLocationData(lat, lng);
                    promptNFCtaped(lat,lng);
                }
            }
        }

        private void NfcStopBtn_Click(object sender, RoutedEventArgs e)
        {
            // Stop NDEF subscription and print status update to the UI
            StopSubscription(true);
        }

        private void StopSubscription(bool writeToStatusOutput)
        {
            if (_subscriptionIdNdef != 0 && _device != null)
            {
                // Ask the proximity device to stop subscribing for NDEF messages
                _device.StopSubscribingForMessage(_subscriptionIdNdef);
                _subscriptionIdNdef = 0;
                // Update enabled / disabled state of buttons in the User Interface
                UpdateUiForNfcStatusAsync();
                // Update status text for UI - only if activated
                if (writeToStatusOutput) LoggingMsg("StatusSubscriptionStopped");
            }
        }

        private string ConvertTypeNameFormatToString(NdefRecord.TypeNameFormatType tnf)
        {
            // Each record contains a type name format, which defines which format
            // the type name is actually in.
            // This method converts the constant to a human-readable string.
            string tnfString;
            switch (tnf)
            {
                case NdefRecord.TypeNameFormatType.Empty:
                    tnfString = "Empty NDEF record (does not contain a payload)";
                    break;
                case NdefRecord.TypeNameFormatType.NfcRtd:
                    tnfString = "NFC RTD Specification";
                    break;
                case NdefRecord.TypeNameFormatType.Mime:
                    tnfString = "RFC 2046 (Mime)";
                    break;
                case NdefRecord.TypeNameFormatType.Uri:
                    tnfString = "RFC 3986 (Url)";
                    break;
                case NdefRecord.TypeNameFormatType.ExternalRtd:
                    tnfString = "External type name";
                    break;
                case NdefRecord.TypeNameFormatType.Unknown:
                    tnfString = "Unknown record type; should be treated similar to content with MIME type 'application/octet-stream' without further context";
                    break;
                case NdefRecord.TypeNameFormatType.Unchanged:
                    tnfString = "Unchanged (partial record)";
                    break;
                case NdefRecord.TypeNameFormatType.Reserved:
                    tnfString = "Reserved";
                    break;
                default:
                    tnfString = "Unknown";
                    break;
            }
            return tnfString;
        }


        private void TrackBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Elderly_TrackingPage));
        }
    }
}
