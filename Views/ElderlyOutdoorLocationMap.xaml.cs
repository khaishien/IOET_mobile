using CaregiverMobile.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
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
    public sealed partial class ElderlyOutdoorLocationMap : Page
    {
        OutdoorModel outdoorModel;

        public ElderlyOutdoorLocationMap()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            outdoorModel = e.Parameter as OutdoorModel;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {

            var locator = new Geolocator();
            if (outdoorModel != null)
            {
                Geopoint myPoint = new Geopoint(new BasicGeoposition()
                {
                    Latitude = Convert.ToDouble(outdoorModel.latitude),
                    Longitude = Convert.ToDouble(outdoorModel.longitude)
                });
                Debug.WriteLine("here");

                //create POI
                MapIcon myPOI = new MapIcon { Location = myPoint, NormalizedAnchorPoint = new Point(0.5, 1.0), Title = "Here", ZIndex = 0 };
                // add to map and center it
                ElderlyLocationMap.MapElements.Add(myPOI);
                ElderlyLocationMap.Center = myPoint;
                ElderlyLocationMap.ZoomLevel = 10;
                await ElderlyLocationMap.TrySetViewAsync(myPoint, 18D);
            }
        }
    }
}
