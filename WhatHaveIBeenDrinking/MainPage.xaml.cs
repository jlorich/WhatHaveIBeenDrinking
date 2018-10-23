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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WhatHaveIBeenDrinking
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
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

        private void OnFaceDetected(object sender, FaceDetectedEventArgs args)
        {
            var face = args.Face;
            Console.Out.WriteLine("Face detected");

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            cameraControl.FaceDetected += OnFaceDetected;

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
