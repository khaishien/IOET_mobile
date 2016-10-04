using CaregiverMobile.Models;
using CaregiverMobile.SpeechClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace CaregiverMobile.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Enrollment3 : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();
        private SpeechServiceClient _serviceClient;

        string subscriptionKey = "d14f2a5acaa64e00bf0732b6bd557d59";
        string VoiceProfileId = "";

        private InMemoryRandomAccessStream buffer;
        private MediaCapture capture;
        private bool Recording;

        StorageFile recordingFile;
        static readonly string RECORDING_FILE = "recording.bin";
        //static readonly string RECORDING_FILE = "audio.wav";
        private string filename;


        public Enrollment3()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _serviceClient = new SpeechServiceClient(subscriptionKey);
            //GetPhrase();
            Step1GuideTB.Foreground = new SolidColorBrush(Colors.Green);

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

        private void CreateProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            CheckProfileExist();
            //CreateProfile();
        }

        private async void CreateProfile()
        {
            progressbar.Visibility = Visibility.Visible;
            try
            {
                LoggingMsg("Creating Speaker Profile...");
                CreateProfileResponse creationResponse = await _serviceClient.CreateProfileAsync("en-us");
                LoggingMsg("Speaker Profile Created.");

                VoiceProfileId = creationResponse.verificationProfileId;
                settings.Values["voiceid"] = VoiceProfileId;

                string phrase = await GetPhrase();
                PhraseTB.Text = phrase;

                if (phrase != null)
                {
                    UpdateUserVoice(VoiceProfileId, phrase);
                }
                LoggingMsg("Voice Id: " + VoiceProfileId);
                LoggingMsg("Chosen phrase: " + phrase);

                LoggingMsg("Retreiving The Created Profile...");
                UpdateProfileStatus(VoiceProfileId);
                LoggingMsg("Speaker Profile Retreived.");
                ProfileCB.IsChecked = true;


                progressbar.Visibility = Visibility.Collapsed;
                CreateProfileBtn.Visibility = Visibility.Collapsed;
                EnrollBtn.Visibility = Visibility.Visible;

            }
            catch (Exception ex)
            {
                progressbar.Visibility = Visibility.Collapsed;
                LoggingMsg("Create profile error: " + ex.Message);
            }

        }

        private async void CheckProfileExist()
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

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        VoiceModel model = JsonConvert.DeserializeObject<VoiceModel>(content);

                        VoiceProfileId = model.speechid;
                        settings.Values["voiceid"] = VoiceProfileId;
                        LoggingMsg("Voice Id: " + VoiceProfileId);
                        LoggingMsg("Retreiving The Created Profile...");
                        UpdateProfileStatus(VoiceProfileId);
                        LoggingMsg("Speaker Profile Retreived.");
                        ProfileCB.IsChecked = true;
                        PhraseTB.Text = model.phrase;


                        progressbar.Visibility = Visibility.Collapsed;
                        EnrollBtn.Visibility = Visibility.Visible;
                        CreateProfileBtn.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        CreateProfile();
                        LoggingMsg("No profile exist...creating profile..");
                    }

                }
                catch
                {
                    LoggingMsg("Error occur...please try again later...");
                    progressbar.Visibility = Visibility.Collapsed;

                }
            }
        }

        private async void UpdateUserVoice(string voiceProfileId, string phrase)
        {
            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                try
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(common.getIP() + "api/updatevoice/" + api.ToString() + "/" + id.ToString() + "/" + voiceProfileId + "/" + phrase);

                    if (response.IsSuccessStatusCode)
                    {

                    }

                }
                catch
                {

                }
            }
        }

        private async void UpdateProfileStatus(string voiceProfileId)
        {
            progressbar.Visibility = Visibility.Visible;
            try
            {
                VerificationProfile profile = await _serviceClient.GetProfileAsync(voiceProfileId);
                enrollmentCountTB.Text = profile.enrollmentsCount.ToString();
                remainingEnrollmentsCountTB.Text = profile.remainingEnrollmentsCount.ToString();
                enrollmentStatusTB.Text = profile.EnrollmentStatus.ToString();

                if(profile.remainingEnrollmentsCount == 0)
                {
                    EnrollBtn.Visibility = Visibility.Collapsed;
                    NextBtn.Visibility = Visibility.Visible;
                    VoiceCB.IsChecked = true;

                }
                Step2GuideTB.Foreground = new SolidColorBrush(Colors.Green);

                progressbar.Visibility = Visibility.Collapsed;
            }
            catch(Exception ex)
            {
                LoggingMsg("Retrieve profile error : " + ex.Message);
            }

        }

        private async Task<string> GetPhrase()
        {
            try
            {
                List<VerificationPhrase> phrases = await _serviceClient.GetVerficationPhrase();
                LoggingMsg("Get phrase successful...");
                Debug.WriteLine("here-" + phrases.Count);

                Random random = new Random();
                int randomNumber = random.Next(0, phrases.Count - 1);

                //PhraseTB.Text = phrases[randomNumber].Phrase;
                return phrases[randomNumber].Phrase;
            }
            catch (Exception ex)
            {
                LoggingMsg("Get Phrase error: " + ex.Message);
                return null;
            }
        }

        private async void EnrollmentProfile()
        {
            progressbar.Visibility = Visibility.Visible;

            using (var stream = await this.recordingFile.OpenReadAsync())
            {
                LoggingMsg("Enrolling...");
                try
                {
                    var reponse = await _serviceClient.EnrollAsync(stream, VoiceProfileId, TimeSpan.FromSeconds(5), 10);
                    LoggingMsg("Enroll successful.");
                    progressbar.Visibility = Visibility.Collapsed;

                    enrollmentCountTB.Text = reponse.enrollmentsCount.ToString();
                    remainingEnrollmentsCountTB.Text = reponse.remainingEnrollments.ToString();
                    enrollmentStatusTB.Text = reponse.EnrollmentStatus.ToString();

                    if (reponse.remainingEnrollments == 0)
                    {
                        EnrollBtn.Visibility = Visibility.Collapsed;
                        NextBtn.Visibility = Visibility.Visible;
                        VoiceCB.IsChecked = true;
                    }
                    else
                    {
                        recordingFile = null;
                    }
                }
                catch (EnrollmentException ex)
                {
                    progressbar.Visibility = Visibility.Collapsed;

                    LoggingMsg("Enrollment voice error: " + ex.Message);
                }
                catch (Exception ex)
                {
                    progressbar.Visibility = Visibility.Collapsed;

                    LoggingMsg("Enrollment custom error: " + ex.Message);

                }


            }

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
        }

        private async void playAudio()
        {
            MediaElement playback = new MediaElement();
            IRandomAccessStream stream = await recordingFile.OpenAsync(FileAccessMode.Read);
            playback.SetSource(stream, recordingFile.FileType);
            playback.Play();
        }
        
        private void EnrollBtn_Click(object sender, RoutedEventArgs e)
        {
            EnrollmentProfile();
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

        private async void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            await Play(Dispatcher);

        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Enrollment4));
        }

        private async void ResetEnrollBtn_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Id:" + VoiceProfileId);
            try
            {
                var status = await _serviceClient.ResetEnrollAsync(VoiceProfileId);
                if (status)
                {
                    UpdateProfileStatus(VoiceProfileId);
                }
            }
            catch (Exception ex)
            {
                LoggingMsg("Reset Enroll error:" + ex.Message);
            }
        }

        //private void Save()
        //{
        //    progressbar.Visibility = Visibility.Visible;
        //    if (buffer != null)
        //    {
        //        IRandomAccessStream audio = buffer.CloneStream();
        //        if (audio == null) throw new ArgumentNullException("buffer");
        //        audioStream = audio.AsStream();
        //        progressbar.Visibility = Visibility.Collapsed;
        //        Step2Mark.Visibility = Visibility.Visible;
        //        EnrollBtn.IsEnabled = true;

        //    }
        //}

    }
}
