using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public sealed partial class Enrollment4 : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

        public Enrollment4()
        {
            this.InitializeComponent();
        }
        private void LoggingMsg(string msg)
        {
            LogBlock.Text = LogBlock.Text + System.Environment.NewLine + msg;
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            settings.Values["enroll"] = true;
            this.Frame.Navigate(typeof(VerifyFace));
        }

        private void FinishEnrollment()
        {
            object personid = settings.Values["personid"];
            object faceid = settings.Values["faceid"];
            object voiceid = settings.Values["voiceid"];

            if (personid != null && faceid != null)
            {
                LoggingMsg("Confirm pesonid of face: " + personid.ToString());
                LoggingMsg("Confirm faceid of face: " + faceid.ToString());
                LoggingMsg("Confirm voiceid of voice: " + voiceid.ToString());
                LoggingMsg("Press next to finish enroll.");
            }
        }
    }
}
