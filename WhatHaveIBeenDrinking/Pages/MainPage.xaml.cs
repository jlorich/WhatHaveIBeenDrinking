using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;

using Windows.Storage;
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
using Windows.UI.Xaml.Media.Imaging;

using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using Newtonsoft.Json;

using WhatHaveIBeenDrinking.Options;
using WhatHaveIBeenDrinking.Controls;
using WhatHaveIBeenDrinking.Repositories;
using WhatHaveIBeenDrinking.Services;
using WhatHaveIBeenDrinking.Entities;
using WhatHaveIBeenDrinking.Pages;
using Windows.UI.Xaml.Media.Animation;

namespace WhatHaveIBeenDrinking {

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {

        private const string SETTINGS_FILE_LOCATION = "appsettings.json";
        private const bool SAVE_SCREEN_GRABS = true;

        private static IConfiguration _Configuration;

        private static IServiceCollection _Services;

        private static IServiceProvider _ServiceProvider;

        private static int PHOTO_TIMER_DURATION = 2000;
        private static int TIMEOUT_TIMER_DURATION = 10000;

        private bool _DetectingFaces = false;


        private Timer _PhotoTimer;

        private Timer _TimeoutTimer;

        public MainPage() {

            InitializeComponent();
            BuildConfiguration();
            ConfigureServices(this);
        }

        public static void ConfigureServices(MainPage mainPage) {

            _Services = new ServiceCollection();

            _Services.AddOptions();
            _Services.Configure<KioskOptions>(_Configuration);
            _Services.AddTransient<KioskRepository>();
            _Services.AddTransient<ImageClassificationService>();
            _Services.AddTransient<KioskService>();

            _ServiceProvider = _Services.BuildServiceProvider();
        }

        public static void BuildConfiguration() {

            var packageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            _Configuration = new ConfigurationBuilder()
                .SetBasePath(packageFolder.Path.ToString())
                .AddJsonFile(SETTINGS_FILE_LOCATION, optional: true, reloadOnChange: true)
                .Build();
        }

        private async void CurrentWindowActivationStateChanged(object sender, Windows.UI.Core.WindowActivatedEventArgs e) {

            if ((e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated ||
                e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.PointerActivated) &&
                cameraControl.CameraStreamState == Windows.Media.Devices.CameraStreamState.Shutdown) {

                // When our Window loses focus due to user interaction Windows shuts it down, so we 
                // detect here when the window regains focus and trigger a restart of the camera.
                await cameraControl.StartStreamAsync(isForRealTimeProcessing: true);
            }
        }

        private async void CheckForDrinks(Object source, ElapsedEventArgs e) {
            _PhotoTimer.Stop();

            try {

                var frame = await cameraControl.GetFrame();

                if (frame == null) {
                    return;
                }

                // Start the tasks to identify the Drink and the User
                await ShowContentIfDrinkDetected(frame);
            }
            catch (Exception /*ex*/) {

            }
            finally
            {
                _PhotoTimer.Start();
            }
        }

        private async Task<bool> ShowContentIfDrinkDetected(SoftwareBitmap frame) {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            { 
                TextBlock_Name.Text = "Analyzing Image";
            });

            var kioskService = _ServiceProvider.GetService<KioskService>();
            var result = await kioskService.IdentifyDrink(frame);

            if (string.IsNullOrEmpty(result?.Drink?.Name))
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    _PhotoTimer.Interval = PHOTO_TIMER_DURATION;
                    TextBlock_Name.Text = "No beverages found";
                });

                return false;
            }

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Frame.Navigate(typeof(DrinkContentPage), result.Drink, new EntranceNavigationTransitionInfo());
            });

            return true;
        }

        private async void OnFaceDetectionStartedAsync(Object sender, EventArgs args) {
            _TimeoutTimer.Stop();

            if (_DetectingFaces)
            {
                return;
            }

            _DetectingFaces = true;
            
            _PhotoTimer.Start();

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                TextBlock_Name.Text = "SEARCHING";
                c1.Width = new GridLength(0);
                c3.Width = new GridLength(0);
                r1.Height = new GridLength(0);
                r3.Height = new GridLength(0);
                TextBlock_Instructions.Visibility = Visibility.Visible;
                Image_Silhouette.Visibility = Visibility.Visible;
            });
        }

        private void OnFacesNoLongerDetected(Object sender, EventArgs args) {
            _TimeoutTimer.Start();
        }

        private async void NoOneIsPresent(Object sender, EventArgs args) {
            if (_DetectingFaces == false)
            {
                return;
            }

            _TimeoutTimer.Stop();
            _PhotoTimer.Stop();

            _DetectingFaces = false;

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                TextBlock_Name.Text = "CALL TO ACTION";
                c1.Width = new GridLength(1, GridUnitType.Star);
                c3.Width = new GridLength(2, GridUnitType.Star);
                r1.Height = new GridLength(1, GridUnitType.Star);
                r3.Height = new GridLength(2, GridUnitType.Star);
                TextBlock_Instructions.Visibility = Visibility.Collapsed;
                Image_Silhouette.Visibility = Visibility.Collapsed;
            });
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _PhotoTimer.Stop();
            _TimeoutTimer.Stop();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e) {

            cameraControl.FaceDetectionStarted += OnFaceDetectionStartedAsync;
            cameraControl.FacesNoLongerDetected += OnFacesNoLongerDetected;

            _PhotoTimer = new Timer(PHOTO_TIMER_DURATION);
            _PhotoTimer.Elapsed += CheckForDrinks;
            _PhotoTimer.AutoReset = true;
            _PhotoTimer.Start();

            _TimeoutTimer = new Timer(TIMEOUT_TIMER_DURATION);
            _TimeoutTimer.Elapsed += NoOneIsPresent;
            _TimeoutTimer.Stop();

            await cameraControl.StartStreamAsync(isForRealTimeProcessing: true);

            base.OnNavigatedTo(e);
        }
    }
}