using CaregiverMobile.Models;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CaregiverMobile.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Enrollment1 : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

        string subscriptionKey = "22a37c301d46459292ce2cf0c0471328";
        string PersonGroupId = "elderly";

        UserModel userModel = new UserModel();
        
        bool CreatePersonSuccess = false;

        public Enrollment1()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            userModel = e.Parameter as UserModel;
        }
        
        private void LoggingMsg(string msg)
        {
            LogBlock.Text = LogBlock.Text + System.Environment.NewLine + msg;
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Enrollment2));
            //if (CreatePersonSuccess == true)
            //{
                
            //}
            //else
            //{
            //    CreatePerson();
            //}

        }

        private async void CreatePerson()
        {
            enroll_progressbar.Visibility = Visibility.Visible;
            NextBtn.IsEnabled = false;
            if (userModel != null)
            {
                try
                {
                    var faceServiceClient = new FaceServiceClient(subscriptionKey);
                    //userdata as user._id
                    CreatePersonResult person = await faceServiceClient.CreatePersonAsync(PersonGroupId, userModel.userfullname, userModel._id);

                    settings.Values["personid"] = person.PersonId;

                    LoggingMsg("Create person success. PersonId: " + person.PersonId);
                    LoggingMsg("Proceed to next step...");
                    enroll_progressbar.Visibility = Visibility.Collapsed;
                    CreatePersonSuccess = true;

                    NextBtn.IsEnabled = true;
                }
                catch (FaceAPIException ex)
                {
                    CreatePersonSuccess = false;
                    Debug.WriteLine("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                    LoggingMsg("Create person group failed. " + ex.ErrorCode + "," + ex.ErrorMessage);

                    NextBtn.IsEnabled = true;
                }


            }
            else
            {
                LoggingMsg("user model is empty.");
                CreatePersonSuccess = false;
            }
        }

        private void SkipVerifyBtn_Click(object sender, RoutedEventArgs e)
        {
            object userrole = settings.Values["userrole"];
            if (userrole != null)
            {
                if (userrole.ToString() == "caregiver")
                {
                    this.Frame.Navigate(typeof(MainPage));
                }
                else
                {
                    this.Frame.Navigate(typeof(Elderly_1_Page));
                }
            }
        }
    }
}
