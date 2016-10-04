using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
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
    public sealed partial class VerifyFace : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

        string face_subscriptionKey = "22a37c301d46459292ce2cf0c0471328";
        string PersonGroupId = "elderly";
        private StorageFile file;
        private Face tempFace = null;
        private Face getFace = null;

        private readonly SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);
        private readonly double lineThickness = 2.0;
        private readonly SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);

        string facefilename = "faceimage";

        bool resultVerify = false;
        private double Confidence = 0;

        public VerifyFace()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.Frame.BackStack.Clear();
        }

        private void LoggingMsg(string msg)
        {
            LogBlock.Text = LogBlock.Text + System.Environment.NewLine + msg;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (resultVerify)
                Frame.BackStack.RemoveAt(Frame.BackStackDepth - 1);
        }

        private async void TakePicture()
        {
            progressbar.Visibility = Visibility.Visible;
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            Size aspectRatio = new Size(16, 9);
            captureUI.PhotoSettings.CroppedAspectRatio = aspectRatio;
            captureUI.PhotoSettings.CroppedSizeInPixels = new Size(0, 0);
            captureUI.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.HighestAvailable;

            file = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (file != null)
            {
                progressbar.Visibility = Visibility.Collapsed;
                LoggingMsg("Load file success. Path: " + file.Path);
                //var stream = await file.OpenAsync(FileAccessMode.Read);
                //var image = new BitmapImage();
                //image.SetSource(stream);

                //ImagePreview.Source = image;
                ClearVisualization();
                CheckFaceByRest();
            }
        }

        private async void CheckFaceByRest()
        {
            if (file != null)
            {
                using (var fileStream = await file.OpenStreamForReadAsync())
                {
                    try
                    {
                        LoggingMsg("Detecting face...");
                        var faceServiceClient = new FaceServiceClient(face_subscriptionKey);
                        Face[] faces = await faceServiceClient.DetectAsync(fileStream);
                        Debug.WriteLine("Response: Success. Detected {0} face(s) in {1}", faces.Length, file.Path);
                        //Debug.WriteLine("Response: Success. Faceid {0}", faces[0].FaceId);

                        var stream = await file.OpenAsync(FileAccessMode.Read);
                        var image = new BitmapImage();
                        image.SetSource(stream);

                        ImageBrush brush = new ImageBrush();
                        brush.ImageSource = image;
                        brush.Stretch = Stretch.Fill;
                        this.VisualizationCanvas.Background = brush;

                        if (faces != null)
                        {
                            double widthScale = image.PixelWidth / this.VisualizationCanvas.ActualWidth;
                            double heightScale = image.PixelHeight / this.VisualizationCanvas.ActualHeight;
                            if (faces.Length > 0)
                            {
                                foreach (Face face in faces)
                                {
                                    Rectangle box = new Rectangle();
                                    box.Tag = face.FaceRectangle;
                                    box.Width = (uint)(face.FaceRectangle.Width / widthScale);
                                    box.Height = (uint)(face.FaceRectangle.Height / heightScale);
                                    box.Fill = this.fillBrush;
                                    box.Stroke = this.lineBrush;
                                    box.StrokeThickness = this.lineThickness;
                                    box.Margin = new Thickness((uint)(face.FaceRectangle.Left / widthScale), (uint)(face.FaceRectangle.Top / heightScale), 0, 0);

                                    this.VisualizationCanvas.Children.Add(box);
                                }
                            }
                        }

                        string message;
                        if (faces == null || faces.Length == 0)
                        {
                            message = "Didn't find any human faces in the image";
                            VerifyBtn.IsEnabled = false;
                        }
                        else if (faces.Length == 1)
                        {
                            message = "Found a human face in the image";
                            VerifyBtn.IsEnabled = true;
                            tempFace = faces[0];
                            LoggingMsg("Faceid : " + faces[0].FaceId);
                        }
                        else
                        {
                            message = "Found " + faces.Length + " human faces in the image";
                            VerifyBtn.IsEnabled = false;
                        }

                        LoggingMsg(message);

                    }
                    catch (FaceAPIException ex)
                    {
                        LoggingMsg("Check face failed. " + ex.ErrorCode + "," + ex.ErrorMessage);
                    }
                    catch(Exception ex)
                    {
                        LoggingMsg("Check face failed. " + ex.Message);
                    }
                }
            }
        }

        private void ImageTakeBtn_Click(object sender, RoutedEventArgs e)
        {
            TakePicture();
        }

        private async void Verify()
        {
            if (tempFace != null)
            {
                VerifyBtn.IsEnabled = false;
                LoggingMsg("Verifying...");
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                Stream s = null;
                //detect
                try
                {
                    
                    s = await storageFolder.OpenStreamForReadAsync(facefilename);

                    var faceServiceClient = new FaceServiceClient(face_subscriptionKey);
                    Face[] faces = await faceServiceClient.DetectAsync(s);
                    Debug.WriteLine("Response: Success. Faceid {0}", faces[0].FaceId);
                    getFace = faces[0];
                }
                catch (FaceAPIException ex)
                {
                    LoggingMsg("Read saved face failed. " + ex.ErrorCode + "," + ex.ErrorMessage);
                    VerifyBtn.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    LoggingMsg("Read saved face failed. " + ex.Message);
                    VerifyBtn.IsEnabled = true;
                }

                if (getFace != null)
                {
                    try
                    {
                        var faceServiceClient = new FaceServiceClient(face_subscriptionKey);
                        VerifyResult vs = await faceServiceClient.VerifyAsync(getFace.FaceId, tempFace.FaceId);

                        //if (vs.IsIdentical)
                        //{
                        //    resultVerify = true;
                        //    LoggingMsg("Response: Success.The face belong to user.");
                        //    Confidence = vs.Confidence;
                        //    LoggingMsg("Confidence: " + vs.Confidence);
                        //    FinishBtn.Visibility = Visibility.Visible;
                        //    VerifyBtn.Visibility = Visibility.Collapsed;
                        //}
                        //else
                        //{
                        //    LoggingMsg("Response: Success.The face not belong to user.");
                        //    LoggingMsg("Confidence: " + vs.Confidence);
                        //    VerifyBtn.IsEnabled = true;
                        //}

                        LoggingMsg("Response from server. Confidence: " + vs.Confidence);
                        Confidence = vs.Confidence;
                        Debug.WriteLine(Confidence);
                        FinishBtn.Visibility = Visibility.Visible;
                        VerifyBtn.Visibility = Visibility.Collapsed;
                    }
                    catch (FaceAPIException ex)
                    {
                        LoggingMsg("Get face failed. " + ex.ErrorCode + "," + ex.ErrorMessage);
                        VerifyBtn.IsEnabled = true;
                    }
                    catch (Exception ex)
                    {
                        LoggingMsg("Get face failed. " + ex.Message);
                        VerifyBtn.IsEnabled = true;
                    }
                }
                else
                {
                    LoggingMsg("Error retreive the image saved.");
                }

            }
            else
            {
                LoggingMsg("Error retreive the image taken.");
            }



        }

        private void ClearVisualization()
        {
            this.VisualizationCanvas.Background = null;
            this.VisualizationCanvas.Children.Clear();
        }

        private void VerifyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (tempFace != null)
            {
                Verify();
            }
        }

        private void FinishBtn_Click(object sender, RoutedEventArgs e)
        {
            //settings.Values["verified"] = true;
            //object userrole = settings.Values["userrole"];
            //if (userrole != null)
            //{
            //    if(userrole.ToString() == "caregiver")
            //    {
            //        this.Frame.Navigate(typeof(MainPage));
            //    }
            //    else
            //    {
            //        this.Frame.Navigate(typeof(Elderly_MainPage));
            //    }
            //}
            Debug.WriteLine(Confidence);

            if (Confidence != 0)
                this.Frame.Navigate(typeof(VerifyVoice),Confidence.ToString());
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
