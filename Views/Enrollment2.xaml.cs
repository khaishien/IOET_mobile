using CaregiverMobile.Models;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
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

    public sealed partial class Enrollment2 : Page
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
        Common common = new Common();

        string subscriptionKey = "22a37c301d46459292ce2cf0c0471328";
        string PersonGroupId = "ioet_face_group";

        private readonly SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);
        private readonly double lineThickness = 2.0;
        private readonly SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
        private readonly uint sourceImageHeightLimit = 1280;

        StorageFile file = null;
        //SoftwareBitmap detectorInput = null;
        //WriteableBitmap displaySource = null;

        bool SyncServerSuccess = false;
        bool CreatePersonSuccess = false;
        bool AddFaceSuccess = false;
        bool CheckExistSuccess = false;

        bool FaceStatus = false;
        StorageFile FaceFile = null;
        string facefilename = "faceimage";
        Face[] faces = null;

        public Enrollment2()
        {
            this.InitializeComponent();
        }

        private void ImageTakeBtn_Click(object sender, RoutedEventArgs e)
        {
            TakePicture();
        }

        private void LoggingMsg(string msg)
        {
            LogBlock.Text = LogBlock.Text + System.Environment.NewLine + msg;
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!FaceStatus)
            {
                SaveFaceImageFile(FaceFile, facefilename, FaceStatusCanvas);
            }
            else if (FaceStatus && !SyncServerSuccess)
            {
                if (!CheckExistSuccess)
                {
                    CheckFaceExistInGroup();
                }
                else if (CheckExistSuccess && !CreatePersonSuccess)
                {
                    CreatePerson();
                }
                else if (CheckExistSuccess && CreatePersonSuccess && !AddFaceSuccess)
                {
                    AddFaceToPerson();
                    CheckTrainPersonGroup();
                }
                else if (CreatePersonSuccess && CheckExistSuccess && AddFaceSuccess)
                {
                    SyncServerSuccess = true;
                    LoggingMsg("Sync with server succcessful.Press next proceed next step.");
                }
                
            }
            else if (FaceStatus && SyncServerSuccess)
            {
                this.Frame.Navigate(typeof(Enrollment3));
            }

        }

        //private async void ReadFileFromStorage()
        //{
        //    enroll_progressbar.Visibility = Visibility.Visible;
        //    FileOpenPicker openPicker = new FileOpenPicker();
        //    openPicker.ViewMode = PickerViewMode.Thumbnail;
        //    openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        //    openPicker.FileTypeFilter.Add(".jpg");
        //    openPicker.FileTypeFilter.Add(".png");
        //    file = await openPicker.PickSingleFileAsync();

        //    if (file != null)
        //    {
        //        enroll_progressbar.Visibility = Visibility.Collapsed;
        //        LoggingMsg("Load file success. Path: " + file.Path);
        //        //var stream = await file.OpenAsync(FileAccessMode.Read);
        //        //var image = new BitmapImage();
        //        //image.SetSource(stream);
        //        DetectFace();
        //    }
        //}

        private void SetupVisualization(WriteableBitmap displaySource, IList<DetectedFace> foundFaces)
        {
            ImageBrush brush = new ImageBrush();
            brush.ImageSource = displaySource;
            brush.Stretch = Stretch.Fill;
            this.VisualizationCanvas.Background = brush;

            if (foundFaces != null)
            {
                double widthScale = displaySource.PixelWidth / this.VisualizationCanvas.ActualWidth;
                double heightScale = displaySource.PixelHeight / this.VisualizationCanvas.ActualHeight;

                foreach (DetectedFace face in foundFaces)
                {
                    Rectangle box = new Rectangle();
                    box.Tag = face.FaceBox;
                    box.Width = (uint)(face.FaceBox.Width / widthScale);
                    box.Height = (uint)(face.FaceBox.Height / heightScale);
                    box.Fill = this.fillBrush;
                    box.Stroke = this.lineBrush;
                    box.StrokeThickness = this.lineThickness;
                    box.Margin = new Thickness((uint)(face.FaceBox.X / widthScale), (uint)(face.FaceBox.Y / heightScale), 0, 0);

                    this.VisualizationCanvas.Children.Add(box);
                }
            }

            string message;
            if (foundFaces == null || foundFaces.Count == 0)
            {
                message = "Didn't find any human faces in the image";
                NextBtn.IsEnabled = false;
            }
            else if (foundFaces.Count == 1)
            {
                message = "Found a human face in the image";
                NextBtn.IsEnabled = true;
            }
            else
            {
                message = "Found " + foundFaces.Count + " human faces in the image";
                NextBtn.IsEnabled = false;
            }

            LoggingMsg(message);
        }

        private void ClearVisualization()
        {
            this.VisualizationCanvas.Background = null;
            this.VisualizationCanvas.Children.Clear();
        }

        private BitmapTransform ComputeScalingTransformForSourceImage(BitmapDecoder sourceDecoder)
        {
            BitmapTransform transform = new BitmapTransform();

            if (sourceDecoder.PixelHeight > this.sourceImageHeightLimit)
            {
                float scalingFactor = (float)this.sourceImageHeightLimit / (float)sourceDecoder.PixelHeight;

                transform.ScaledWidth = (uint)Math.Floor(sourceDecoder.PixelWidth * scalingFactor);
                transform.ScaledHeight = (uint)Math.Floor(sourceDecoder.PixelHeight * scalingFactor);
            }

            return transform;
        }

        private async void TakePicture()
        {
            enroll_progressbar.Visibility = Visibility.Visible;
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            Size aspectRatio = new Size(16, 9);
            captureUI.PhotoSettings.CroppedAspectRatio = aspectRatio;
            captureUI.PhotoSettings.CroppedSizeInPixels = new Size(0, 0);
            captureUI.PhotoSettings.MaxResolution = CameraCaptureUIMaxPhotoResolution.HighestAvailable;

            file = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (file != null)
            {
                enroll_progressbar.Visibility = Visibility.Collapsed;
                LoggingMsg("Load file success. Path: " + file.Path);
                //var stream = await file.OpenAsync(FileAccessMode.Read);
                //var image = new BitmapImage();
                //image.SetSource(stream);

                CheckFaceByRest();
            }
        }

        //private async void DetectFace()
        //{
        //    if (file != null)
        //    {
        //        this.ClearVisualization();
        //        LoggingMsg("Opening...");

        //        using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
        //        {
        //            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
        //            BitmapTransform transform = this.ComputeScalingTransformForSourceImage(decoder);


        //            using (SoftwareBitmap originalBitmap = await decoder.GetSoftwareBitmapAsync(decoder.BitmapPixelFormat, BitmapAlphaMode.Ignore, transform, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage))
        //            {
        //                // We need to convert the image into a format that's compatible with FaceDetector.
        //                // Gray8 should be a good type but verify it against FaceDetector’s supported formats.
        //                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Gray8;
        //                if (FaceDetector.IsBitmapPixelFormatSupported(InputPixelFormat))
        //                {
        //                    using (detectorInput = SoftwareBitmap.Convert(originalBitmap, InputPixelFormat))
        //                    {
        //                        // Create a WritableBitmap for our visualization display; copy the original bitmap pixels to wb's buffer.
        //                        displaySource = new WriteableBitmap(originalBitmap.PixelWidth, originalBitmap.PixelHeight);
        //                        originalBitmap.CopyToBuffer(displaySource.PixelBuffer);

        //                        LoggingMsg("Detecting...");

        //                        FaceDetector detector = await FaceDetector.CreateAsync();
        //                        faces = await detector.DetectFacesAsync(detectorInput);

        //                        // Create our display using the available image and face results.
        //                        this.SetupVisualization(displaySource, faces);
        //                    }
        //                }
        //                else
        //                {
        //                    LoggingMsg("PixelFormat '" + InputPixelFormat.ToString() + "' is not supported by FaceDetector");
        //                }
        //            }
        //        }
        //    }
        //}

        private async void CheckFaceByRest()
        {
            if (file != null)
            {
                using (var fileStream = await file.OpenStreamForReadAsync())
                {
                    try
                    {
                        LoggingMsg("Detecting...");
                        var faceServiceClient = new FaceServiceClient(subscriptionKey);
                        faces = await faceServiceClient.DetectAsync(fileStream);
                        Debug.WriteLine("Response: Success. Detected {0} face(s) in {1}", faces.Length, file.Path);
                        //Debug.WriteLine("Response: Success. Faceid {0}", faces[0].FaceId);

                        var stream = await file.OpenAsync(FileAccessMode.Read);
                        var image = new BitmapImage();
                        image.SetSource(stream);

                        ImageBrush brush = new ImageBrush();
                        brush.ImageSource = image;
                        brush.Stretch = Stretch.Fill;
                        this.VisualizationCanvas.Background = brush;

                        if (faces != null && faces.Length > 0)
                        {
                            double widthScale = image.PixelWidth / this.VisualizationCanvas.ActualWidth;
                            double heightScale = image.PixelHeight / this.VisualizationCanvas.ActualHeight;

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

                        string message;
                        if (faces == null || faces.Length == 0)
                        {
                            message = "Didn't find any human faces in the image";
                            NextBtn.IsEnabled = false;
                        }
                        else if (faces.Length == 1)
                        {
                            message = "Found a human face in the image";
                            NextBtn.IsEnabled = true;
                        }
                        else
                        {
                            message = "Found " + faces.Length + " human faces in the image";
                            NextBtn.IsEnabled = false;
                        }

                        LoggingMsg(message);

                    }
                    catch (FaceAPIException ex)
                    {
                        LoggingMsg("Detect face failed. " + ex.ErrorCode + "," + ex.ErrorMessage);
                    }
                    catch (Exception ex)
                    {
                        LoggingMsg("Detect face failed. " + ex.Message);
                    }
                }
            }
        }

        private async void SaveFaceImageFile(StorageFile faceFile, string filename, Grid canvas)
        {
            try
            {
                if (file != null)
                {
                    LoggingMsg("Saving image to local file...");
                    StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                    faceFile = await storageFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

                    NextBtn.IsEnabled = false;

                    await file.CopyAndReplaceAsync(faceFile);
                    //Debug.WriteLine(faceFile.Path);
                    canvas.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    NextBtn.IsEnabled = true;
                    //file = null;
                    //ClearVisualization();

                    FaceStatus = true;
                    //if (filename == face1filename)
                    //    Face1Status = true;
                    //else if (filename == face2filename)
                    //    Face2Status = true;
                    //else if (filename == face3filename)
                    //{
                    //    Face3Status = true;
                    //    NextBtn.IsEnabled = true;
                    //}

                    LoggingMsg("Save file success. Proceed next step.");
                }
                else
                {
                    LoggingMsg("Please take picture or input image.");
                }

            }
            catch (Exception ex)
            {
                LoggingMsg("Error saving " + filename + " : " + ex.Message);
            }


        }

        private async void SyncServer()
        {
            Object api = settings.Values["api"];
            Object id = settings.Values["userid"];
            if (api != null && id != null)
            {
                if (file != null)
                {
                    try
                    {
                        var httpClient = new HttpClient();
                        //var response = await httpClient.GetAsync(common.getIP() + "api/updatelogin/" + api.ToString() + "/" + id.ToString());

                        byte[] b = File.ReadAllBytes(file.Path);

                        using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString()))
                        {
                            content.Add(new StreamContent(new MemoryStream(b)), "faceimage", id.ToString());

                            var response = await httpClient.PostAsync(common.getIP() + "api/updateface/" + api.ToString() + "/" + id.ToString(), content);

                            if (response.IsSuccessStatusCode)
                            {

                            }
                        }


                    }
                    catch
                    {

                    }
                }



            }
        }

        private async void CreatePerson()
        {
            Object userid = settings.Values["userid"];

            if (userid != null)
            {
                NextBtn.IsEnabled = false;
                LoggingMsg("Creating face person in server...");
                try
                {
                    var faceServiceClient = new FaceServiceClient(subscriptionKey);
                    //create persongroud
                    //await faceServiceClient.CreatePersonGroupAsync(PersonGroupId, "IOET Face Group");

                    //create person
                    CreatePersonResult person = await faceServiceClient.CreatePersonAsync(PersonGroupId, userid.ToString());
                    if (person != null)
                    {
                        LoggingMsg("Person created successful.");
                        settings.Values["personid"] = person.PersonId;
                        CreatePersonSuccess = true;
                        NextBtn.IsEnabled = true;
                    }

                    LoggingMsg("Created user face library successful , " + person.PersonId);
                }
                catch (FaceAPIException ex)
                {
                    NextBtn.IsEnabled = false;
                    LoggingMsg("Person create failed. Try again. FACEAPI :" + ex.ErrorCode + "," + ex.ErrorMessage);
                }
                catch (Exception ex)
                {
                    NextBtn.IsEnabled = false;
                    LoggingMsg("Person create failed. Try again. " + ex.Message);
                }
            }
        }

        private async void AddFaceToPerson()
        {
            Object userid = settings.Values["userid"];
            Object personid = settings.Values["personid"];


            if (userid != null && personid != null)
            {
                enroll_progressbar.Visibility = Visibility.Visible;
                NextBtn.IsEnabled = false;

                if (file != null)
                {
                    using (var fileStream = await file.OpenStreamForReadAsync())
                    {
                        LoggingMsg("Add face for person to server...");
                        try
                        {
                            var faceServiceClient = new FaceServiceClient(subscriptionKey);
                            AddPersistedFaceResult addPersistedFaceResult = await faceServiceClient.AddPersonFaceAsync(PersonGroupId, (Guid)personid, fileStream);
                            Guid FaceId = addPersistedFaceResult.PersistedFaceId;
                            if (FaceId != null)
                            {
                                settings.Values["faceid"] = FaceId;
                                LoggingMsg("Sync face image with server success. FaceId = " + FaceId);
                                NextBtn.IsEnabled = true;
                                AddFaceSuccess = true;
                            }

                            //start training
                            await faceServiceClient.TrainPersonGroupAsync(PersonGroupId);

                            enroll_progressbar.Visibility = Visibility.Collapsed;
                        }
                        catch (FaceAPIException ex)
                        {
                            enroll_progressbar.Visibility = Visibility.Collapsed;
                            NextBtn.IsEnabled = false;
                            Debug.WriteLine("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                            LoggingMsg("Sync face for person to server failed.");
                        }
                        catch
                        {
                            enroll_progressbar.Visibility = Visibility.Collapsed;
                            NextBtn.IsEnabled = false;
                            LoggingMsg("Sync face for person to server failed.");

                        }
                    }

                }
                else
                {
                    //  file missing
                    LoggingMsg("Image file missing. Please choose image again.");
                    NextBtn.IsEnabled = false;
                    enroll_progressbar.Visibility = Visibility.Collapsed;
                }



            }

        }

        private async void CheckTrainPersonGroup()
        {
            enroll_progressbar.Visibility = Visibility.Visible;
            try
            {
                NextBtn.IsEnabled = false;
                TrainingStatus trainingStatus = null;
                while (true)
                {
                    var faceServiceClient = new FaceServiceClient(subscriptionKey);
                    trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(PersonGroupId);

                    if (trainingStatus.Status != Status.Running)
                    {
                        break;
                    }

                    await Task.Delay(1000);
                }

                if (trainingStatus.Status == Status.Succeeded)
                {
                    LoggingMsg("Face training for " + PersonGroupId + " success.");
                    LoggingMsg("Press for next step...");
                    NextBtn.IsEnabled = true;
                    enroll_progressbar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NextBtn.IsEnabled = false;
                    LoggingMsg("Face training for " + PersonGroupId + " failed. Try again later.");
                    enroll_progressbar.Visibility = Visibility.Collapsed;
                }
            }
            catch (FaceAPIException ex)
            {
                NextBtn.IsEnabled = false;
                enroll_progressbar.Visibility = Visibility.Collapsed;
                Debug.WriteLine("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
            }
        }

        private async void CheckFaceExistInGroup()
        {
            NextBtn.IsEnabled = false;
            LoggingMsg("Checking face exist in server...");
            try
            {
                using (var s = await file.OpenStreamForReadAsync())
                {
                    var faceServiceClient = new FaceServiceClient(subscriptionKey);

                    //faces = await faceServiceClient.DetectAsync(s);
                    var faceIds = faces.Select(face => face.FaceId).ToArray();

                    var results = await faceServiceClient.IdentifyAsync(PersonGroupId, faceIds);

                    if (results.Length > 0)
                    {
                        foreach (var identifyResult in results)
                        {
                            Debug.WriteLine("Check Face Id : " + identifyResult.FaceId);
                            if (identifyResult.Candidates.Length == 0)
                            {
                                //Console.WriteLine("No one identified");
                                CheckExistSuccess = true;
                                LoggingMsg("This face is valid. Proceed to next step.");
                                NextBtn.IsEnabled = true;
                            }
                            else
                            {
                                // Get top 1 among all candidates returned
                                var candidateId = identifyResult.Candidates[0].PersonId;
                                var person = await faceServiceClient.GetPersonAsync(PersonGroupId, candidateId);
                                //Console.WriteLine("Identified as {0}", person.Name);
                                LoggingMsg("Identified as " + person.PersonId + "," + person.Name);
                                
                                Object userid = settings.Values["userid"];

                                if (userid != null)
                                {
                                    if(userid.ToString() == person.Name)
                                    {
                                        settings.Values["personid"] = person.PersonId;
                                        CreatePersonSuccess = true;
                                        CheckExistSuccess = true;
                                        NextBtn.IsEnabled = true;
                                        LoggingMsg("Face will add to exist person.");
                                    }
                                    else
                                    {
                                        LoggingMsg("Face is exist in group. Duplicate face is not allowed!");
                                        NextBtn.IsEnabled = false;
                                    }
                                }
                                
                            }
                        }
                    }
                    else
                    {
                        CheckExistSuccess = true;
                        LoggingMsg("This face is valid. Proceed to next step.");
                        NextBtn.IsEnabled = true;
                    }


                }
            }
            catch (FaceAPIException ex)
            {
                NextBtn.IsEnabled = false;
                LoggingMsg("Check exist face err : " + ex.ErrorMessage);
            }
            catch (Exception ex)
            {
                NextBtn.IsEnabled = false;
                LoggingMsg("Check exist face err : " + ex.Message);
            }


        }
    }
}
