using CaregiverMobile.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Calls;
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
    public sealed partial class Elderly_1_Page : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();

        public Elderly_1_Page()
        {
            this.InitializeComponent();
        }

        private async void CallHelpBtn_Click(object sender, RoutedEventArgs e)
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/alert/" + api.ToString() + "/" + id.ToString() + "/1");

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("sent.");
                        RequestCaregiverNo();
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

        private async void RequestCaregiverNo()
        {
            object api = settings.Values["api"];
            object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/elderly/caregiverno/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        List<UserModel> a = JsonConvert.DeserializeObject<List<UserModel>>(content);
                        

                        Random rnd = new Random();
                        int num = rnd.Next(a.Count);

                        PhoneCallManager.ShowPhoneCallUI(a[num].usercontact, a[num].userfullname);

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

        private void MainPageBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Elderly_MainPage));
        }
    }
}
