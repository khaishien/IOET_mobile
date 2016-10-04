using CaregiverMobile.Models;
using CaregiverMobile.SpeechClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
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
    public sealed partial class VerifyVoice : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();
        private SpeechServiceClient _serviceClient;

        string subscriptionKey = "d14f2a5acaa64e00bf0732b6bd557d59";

        private InMemoryRandomAccessStream buffer;
        private MediaCapture capture;
        private bool Recording;

        StorageFile recordingFile;
        static readonly string RECORDING_FILE = "recording.bin";
        private string filename;
        double face_confidence = 0;

        public VerifyVoice()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _serviceClient = new SpeechServiceClient(subscriptionKey);

            LoadProfile();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            face_confidence = Convert.ToDouble(e.Parameter as String);
            //string c = e.Parameter as String;
            Debug.WriteLine("1:"+face_confidence);
            //Debug.WriteLine("2:"+c);
            if (face_confidence == 0)
                this.Frame.GoBack();

        }

        private void RecordBtn_Click(object sender, RoutedEventArgs e)
        {
            PhraseTB.Foreground = new SolidColorBrush(Colors.Green);
            LoggingMsg("please ready for read the phrase displayed");
            Record();
            TimeSpan delay = TimeSpan.FromSeconds(5);
            ThreadPoolTimer DelayTimer = ThreadPoolTimer.CreateTimer(
            (source) => {
                Stop();
            }, delay);
            Load5SecProgress();
            VerifyBtn.IsEnabled = true;
        }
        private async void Load5SecProgress()
        {
            recordprogressbar.Maximum = 4;
            for (int i = 0; i < 5; i++)
            {
                recordprogressbar.Value = i;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

        }

        private async void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            await Play(Dispatcher);
        }

        private void VerifyBtn_Click(object sender, RoutedEventArgs e)
        {
            Verify();
        }

        private void FinishBtn_Click(object sender, RoutedEventArgs e)
        {
            settings.Values["verified"] = true;
            settings.Values["loginTime"] = DateTime.Now.ToLocalTime().ToString();

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

        private async void LoadProfile()
        {
            progressbar.Visibility = Visibility.Visible;

            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/getvoice/" + api.ToString() + "/" + id.ToString());
                    progressbar.Visibility = Visibility.Collapsed;

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        VoiceModel model = JsonConvert.DeserializeObject<VoiceModel>(content);

                        
                        LoggingMsg("Retreiving The Created Profile...");
                        Boolean status = await GetStatusProfile(model.speechid);

                        LoggingMsg("Speaker Profile Retreived.");
                        if (status)
                        {
                            PhraseTB.Text = model.phrase;
                        }
                        else
                        {
                            LoggingMsg("Profile still training...Try again later...");
                        }
                        
                    }
                    else
                    {
                        LoggingMsg("Retrieve Profile error...");
                    }

                }
                catch
                {
                    LoggingMsg("Error occur...please try again later...");
                    progressbar.Visibility = Visibility.Collapsed;

                }
            }
        }

        private async Task<Boolean> GetStatusProfile(string voiceProfileId)
        {
            progressbar.Visibility = Visibility.Visible;
            try
            {
                Debug.WriteLine("123"+voiceProfileId);

                VerificationProfile profile = await _serviceClient.GetProfileAsync(voiceProfileId);
                progressbar.Visibility = Visibility.Collapsed;
                if (profile.EnrollmentStatus == EnrollmentStatus.Enrolled)
                {
                    return true;
                }
                else
                {
                    return false;
                }

                
            }
            catch (Exception ex)
            {
                LoggingMsg("Retrieve profile error : " + ex.Message);
                return false;
            }
        }

        private async void Verify()
        {
            progressbar.Visibility = Visibility.Visible;

            string voiceid = (string)settings.Values["voiceid"];
            using (var stream = await this.recordingFile.OpenReadAsync())
            {
                LoggingMsg("Verifying...");
                try
                {
                    var reponse = await _serviceClient.VerifyAsync(stream, voiceid, TimeSpan.FromSeconds(5), 10);
                    LoggingMsg("Verify successful.");

                    progressbar.Visibility = Visibility.Collapsed;
                    LoggingMsg("Result accepted...Confident = " + reponse.confidence);

                    computeConfidence(face_confidence, reponse.confidence);
                
                }catch(Exception ex)
                {
                    LoggingMsg("Verify err :" + ex.Message);
                }

            }
        }

        private async void computeConfidence(double face_confidence, VerificationConfidence confidence)
        {
            double voice_confidence = 0;
            if (confidence == VerificationConfidence.High)
                voice_confidence = 0.75;
            else if (confidence == VerificationConfidence.Normal)
                voice_confidence = 0.50;
            else
                voice_confidence = 0.25;

            Debug.WriteLine(face_confidence);
            Debug.WriteLine(voice_confidence);

            double result = face_confidence * 0.7 + voice_confidence * 0.3;
            Debug.WriteLine(result);
            if (result > 0.5)
            {
                FinishBtn.Visibility = Visibility.Visible;
                VerifyBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageDialog md = new MessageDialog("Biometric confidence is low. Try again.");
                await md.ShowAsync();

                if (Frame.CanGoBack)
                    Frame.GoBack();
            }


        }

        private async Task<bool> init()
        {
            if (buffer != null)
            {
                buffer.Dispose();
            }
            buffer = new InMemoryRandomAccessStream();
            if (capture != null)
            {
                capture.Dispose();
            }
            try
            {
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio
                };
                capture = new MediaCapture();
                await capture.InitializeAsync(settings);
                capture.RecordLimitationExceeded += (MediaCapture sender) =>
                {
                    Stop();
                    throw new Exception("Exceeded Record Limitation");
                };
                capture.Failed += (MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs) =>
                {
                    Recording = false;
                    throw new Exception(string.Format("Code: {0}. {1}", errorEventArgs.Code, errorEventArgs.Message));
                };
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.GetType() == typeof(UnauthorizedAccessException))
                {
                    throw ex.InnerException;
                }
                LoggingMsg("Error record: " + ex.Message);
                throw;
            }
            return true;
        }

        public async void Record()
        {
            await init();
            LoggingMsg("Recording...");
            var profile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Low);
            profile.Audio = AudioEncodingProperties.CreatePcm(16000, 1, 16);

            await capture.StartRecordToStreamAsync(profile, buffer);
            if (Recording) throw new InvalidOperationException("cannot excute two records at the same time");
            Recording = true;
        }

        public async void Stop()
        {
            await capture.StopRecordAsync();
            LoggingMsg("Stop recording...");
            Recording = false;
        }

        public async Task Play(CoreDispatcher dispatcher)
        {
            LoggingMsg("Playing audio...");
            MediaElement playback = new MediaElement();
            IRandomAccessStream audio = buffer.CloneStream();
            if (audio == null) throw new ArgumentNullException("buffer");
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            if (!string.IsNullOrEmpty(filename))
            {
                StorageFile original = await storageFolder.GetFileAsync(filename);
                await original.DeleteAsync();
            }
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                recordingFile = await storageFolder.CreateFileAsync(RECORDING_FILE, CreationCollisionOption.ReplaceExisting);
                filename = recordingFile.Name;
                using (IRandomAccessStream fileStream = await recordingFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await RandomAccessStream.CopyAndCloseAsync(audio.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
                    await audio.FlushAsync();
                    audio.Dispose();
                }

                IRandomAccessStream stream = await recordingFile.OpenAsync(FileAccessMode.Read);
                playback.SetSource(stream, recordingFile.FileType);
                //Time.Text = playback.NaturalDuration.TimeSpan.TotalSeconds.ToString();
                playback.Play();
            });
        }

        private async void LoggingMsg(string msg)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.High,
                        () =>
                        {
                            LogBlock.Text = LogBlock.Text + Environment.NewLine + msg;
                        });

            //ScrollLogBlock.ChangeView(null, ScrollLogBlock.ExtentHeight , null, true);

        }

    }
}
