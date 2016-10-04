using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class Elderly_TrackingPage : Page
    {
        public Elderly_TrackingPage()
        {
            this.InitializeComponent();
        }

        private void Request_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Elderly_RequestList));
        }

        private void Current_Monitor_List_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ELderly_MonitorList));
        }

        private void Send_Request_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Elderly_SendRequestPage));
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
