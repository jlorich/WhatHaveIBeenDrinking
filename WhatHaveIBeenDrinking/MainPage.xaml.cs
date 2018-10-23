using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using WhatHaveIBeenDrinking.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Prediction.Models;
using Windows.Storage;
using Newtonsoft.Json;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409


// image uri https://southcentralus.api.cognitive.microsoft.com/customvision/v2.0/Prediction/8f150ff3-f5a9-4fca-88e8-d307c094e10a/image?iterationId=5a41a5ab-ed3e-4c25-aebe-eb00c24c5be0
namespace WhatHaveIBeenDrinking
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private const string SETTINGS_FILE_LOCATION = "appsettings.json";
        private WhatHaveIBeenDrinkingSettings _Settings;

        private WhatHaveIBeenDrinkingSettings Settings
        {
            get
            {
                if (_Settings != null)
                {
                    return _Settings;
                }

                var packageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var file = packageFolder.GetFileAsync(SETTINGS_FILE_LOCATION).GetAwaiter().GetResult();
                var data = FileIO.ReadTextAsync(file).GetAwaiter().GetResult();
                _Settings = JsonConvert.DeserializeObject<WhatHaveIBeenDrinkingSettings>(data);

                return _Settings;
            }
        }


        public MainPage()
        {
            InitializeComponent();
            //var cameraCaptureService = new CameraCaptureService();
            //var faceRecognitionService = new FaceRecognitionService();

            //var photo = cameraCaptureService.Capture().GetAwaiter().GetResult();
            //faceRecognitionService.Recognize(photo);
        }

        private async void CurrentWindowActivationStateChanged(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if ((e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated ||
                e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.PointerActivated) &&
                cameraControl.CameraStreamState == Windows.Media.Devices.CameraStreamState.Shutdown)
            {
                // When our Window loses focus due to user interaction Windows shuts it down, so we 
                // detect here when the window regains focus and trigger a restart of the camera.
                await cameraControl.StartStreamAsync(isForRealTimeProcessing: true);
            }
        }

        private void OnFaceDetectedAsync(object sender, FaceDetectedEventArgs args)
        {
            Console.Out.WriteLine("Face detected");

            PredictImage(args.Bitmap).GetAwaiter().GetResult();

        }

        private async Task PredictImage(SoftwareBitmap bitmap)
        {
            var endpoint = new PredictionEndpoint()
            {
                ApiKey = Settings.CognitiveServicesCustomVisionApiKey
            };

            using (var stream = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                encoder.SetSoftwareBitmap(bitmap);

                await encoder.FlushAsync();

                var result = endpoint.PredictImage(
                    Settings.CognitiveServicesCustomVisionProjectId,
                    stream.AsStream(),
                    Settings.CognitiveServicesCustomVisionIterationId
                );

                double highestProbability = 0;
                ImageTagPredictionModel predictedModel = null;

                foreach(var prediction in result.Predictions)
                {
                    if (prediction.Probability > highestProbability)
                    {
                        highestProbability = prediction.Probability;
                        predictedModel = prediction;
                    }
                }
                
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (predictedModel != null && highestProbability > .25)
                    {
                        CurrentDrinkName.Text = $"{predictedModel.Tag} - {predictedModel.Probability}";
                    } else
                    {
                        CurrentDrinkName.Text = "No drinks detected";
                    }
                });
                
            }
        }



        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            cameraControl.FaceDetected += OnFaceDetectedAsync;

            await cameraControl.StartStreamAsync(isForRealTimeProcessing: true);

            //EnterKioskMode();

            //if (string.IsNullOrEmpty(SettingsHelper.Instance.FaceApiKey))
            //{
            //    await new MessageDialog("Missing Face API Key. Please enter a key in the Settings page.", "Missing API Key").ShowAsync();
            //}
            //else
            //{
            //    FaceListManager.FaceListsUserDataFilter = SettingsHelper.Instance.WorkspaceKey + "_RealTime";
            //    await FaceListManager.Initialize();

            //    await ResetDemographicsData();
            //    this.UpdateDemographicsUI();

                
            //    this.StartProcessingLoop();
            //}

            ////get a reference to SenseHat
            //try
            //{
            //    senseHat = await SenseHatFactory.GetSenseHat();

            //    senseHat.Display.Clear();
            //    senseHat.Display.Update();

            //}
            //catch (Exception)
            //{
            //}

            base.OnNavigatedTo(e);
        }
    }
}
