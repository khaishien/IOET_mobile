using CaregiverMobile.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    public sealed partial class NotificationPage : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();
        string to_user_id = null;
        List<TrackListModel> tracklist;
        ObservableCollection<ChatModel> items  = new ObservableCollection<ChatModel>();

        public NotificationPage()
        {
            this.InitializeComponent();
            LoadUserList();
        }

        private async void LoadUserList()
        {
            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];
            Object userrole = settings.Values["userrole"];

            if (api != null && id != null && userrole != null)
            {
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/chatlist/" + userrole.ToString() + "/" + api.ToString() + "/" + id.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        tracklist = JsonConvert.DeserializeObject<List<TrackListModel>>(content);

                        if(userrole.ToString() == "elderly")
                        {
                            CaregiverLvBinding.ItemsSource = tracklist;
                            CaregiverListGrid.Visibility = Visibility.Visible;
                            ElderlyListGrid.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            ElderlyLvBinding.ItemsSource = tracklist;
                            CaregiverListGrid.Visibility = Visibility.Collapsed;
                            ElderlyListGrid.Visibility = Visibility.Visible;
                        }


                    }
                    else
                    {
                        MessageDialog md = new MessageDialog("Error occur loading user list. Try again later.");
                        await md.ShowAsync();
                    }
                }
                catch
                {
                    MessageDialog md = new MessageDialog("Error occur loading user list. Try again later.");
                    await md.ShowAsync();
                }

            }
        }

        private void UserChatbtn_Click(object sender, RoutedEventArgs e)
        {
            to_user_id = ((Button)sender).CommandParameter.ToString();
            Object userrole = settings.Values["userrole"];

            if(userrole != null)
            {
                if(userrole.ToString() == "elderly")
                {
                    foreach (var user in tracklist)
                    {
                        if (to_user_id == user.caregiverid._id)
                        {
                            ToUserChatTB.Text = user.caregiverid.userfullname;
                        }
                    }
                }
                else
                {
                    foreach (var user in tracklist)
                    {
                        if (to_user_id == user.elderlyid._id)
                        {
                            ToUserChatTB.Text = user.elderlyid.userfullname;
                        }
                    }
                }
                
            }
            
            ElderlyListGrid.Visibility = Visibility.Collapsed;
            CaregiverListGrid.Visibility = Visibility.Collapsed;
            ChatGrid.Visibility = Visibility.Visible;


            LoadChatList();

        }

        private async void LoadChatList()
        {
            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];

            if (api != null && id != null)
            {

                load_progressbar.Visibility = Visibility.Visible;
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/chatcontent/" + api.ToString() + "/" + id.ToString() + "/" + to_user_id);

                    load_progressbar.Visibility = Visibility.Collapsed;

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        List<ChatModel> chatlist = JsonConvert.DeserializeObject<List<ChatModel>>(content);


                        foreach (var chat in chatlist)
                        {
                            if (chat.to._id == id.ToString())
                            {
                                chat.alignment = "Left";
                            }
                            else
                            {
                                chat.alignment = "Right";
                            }

                            items.Add(chat);
                        }

                        ChatLvBinding.ItemsSource = items;

                    }
                    else
                    {
                        MessageDialog md = new MessageDialog("Error occur loading notification list. Try again later.");
                        await md.ShowAsync();
                    }

                }
                catch
                {
                    load_progressbar.Visibility = Visibility.Collapsed;

                    MessageDialog md = new MessageDialog("Error occur loading notification list. Try again later.");
                    await md.ShowAsync();
                }
            }
        }

        private async void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (to_user_id != null)
            {
                Object api = settings.Values["api"];
                Object id = settings.Values["userid"];

                var notification = new SendChatModel()
                {
                    to = to_user_id,
                    from = id.ToString(),
                    content = NotificationContentTB.Text,
                    timestamp = new DateTime()
                };

                
                if (api != null && id != null)
                {
                    try
                    {
                        var httpClient = new HttpClient();
                        var response = await httpClient.PostAsJsonAsync(common.getIP() + "api/sendchat/" + api.ToString() + "/" + id.ToString() , notification);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var n = new ChatModel()
                            {
                                content = NotificationContentTB.Text,
                                alignment = "Right"
                            };

                            items.Add(n);
                        }
                        else
                        {
                            MessageDialog md = new MessageDialog("Error occur send notification. Try again later.");
                            await md.ShowAsync();
                        }
                        NotificationContentTB.Text = "";
                    }
                    catch
                    {
                        NotificationContentTB.Text = "";

                        MessageDialog md = new MessageDialog("Error occur send notification. Try again later.");
                        await md.ShowAsync();
                    }
                }
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {

            Object userrole = settings.Values["userrole"];
            if(userrole != null)
            {
                if(userrole.ToString() == "elderly")
                {
                    ElderlyListGrid.Visibility = Visibility.Collapsed;
                    CaregiverListGrid.Visibility = Visibility.Visible;
                    ChatGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ElderlyListGrid.Visibility = Visibility.Visible;
                    CaregiverListGrid.Visibility = Visibility.Collapsed;
                    ChatGrid.Visibility = Visibility.Collapsed;
                }
            }

            NotificationContentTB.Text = "";
            items.Clear();
        }
    }
}
