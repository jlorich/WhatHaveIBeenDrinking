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
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Windows.Storage;
using Newtonsoft.Json;
using WhatHaveIBeenDrinking.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatHaveIBeenDrinking.Repositories;
using WhatHaveIBeenDrinking.Services;
using Windows.UI.Xaml.Media.Imaging;
using System.Timers;
using System.Diagnostics;

namespace WhatHaveIBeenDrinking {

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {

        private const string SETTINGS_FILE_LOCATION = "appsettings.json";

        private static IConfiguration _Configuration;

        private static IServiceCollection _Services;

        private static IServiceProvider _ServiceProvider;

        private Timer _PhotoTimer;

        private Timer _TimeoutTimer;

        public MainPage() {

            InitializeComponent();
            BuildConfiguration();
            ConfigureServices(this);

            // DEBUG: Use the space bar to invoke execution
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
        }

        private async void CoreWindow_KeyUp(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args) {

            if (args.VirtualKey == Windows.System.VirtualKey.Space) {
                await this.CheckForDrinks();
            }
        }

        public static void ConfigureServices(MainPage mainPage) {

            _Services = new ServiceCollection();

            _Services.AddOptions();
            _Services.Configure<KioskOptions>(_Configuration);
            _Services.AddTransient<KioskRepository>();
            _Services.AddTransient<ImageClassificationService>();
            _Services.AddTransient<IUserService, UserService>();
            _Services.AddTransient<KioskService>();

            _Services.AddTransient<IFaceClient>(sp => {

                var client = new FaceClient(
                    new ApiKeyServiceClientCredentials(_Configuration.GetValue<string>("CognitiveServicesFaceApiKey")),
                    new System.Net.Http.DelegatingHandler[] { }
                );

                client.Endpoint = _Configuration.GetValue<string>("CognitiveServicesFaceEndpoint");

                return client;
            });

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

        private void StartTimer() {
            _PhotoTimer.Start();
        }

        private void StopTimer() {
            _PhotoTimer.Stop();
        }

        private async void OnTimerElapsed(Object source, ElapsedEventArgs e) {
            await CheckForDrinks();
        }

        private async Task CheckForDrinks() {

            try {

                var frame = await cameraControl.GetFrame();

                if (frame == null) {
                    return;
                }

                // Start the tasks to identify the Drink and the User
                var drinkTask = this.IdentifyDrink(frame);
                var userTask = this.IdentifyUser(frame);

                // Make sure both Tasks finish
                await Task.WhenAll(drinkTask, userTask);
            }
            catch (Exception /*ex*/) {

            }
        }

        private async Task IdentifyDrink(SoftwareBitmap frame) {

            var kioskService = _ServiceProvider.GetService<KioskService>();
            var result = await kioskService.IdentifyDrink(frame);

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {

                if (string.IsNullOrEmpty(result?.Name)) {
                    //TextBlock_Name.Text = "Searching for beers..." + result.IdentifiedTag;
                    TextBlock_Description.Text = "";
                    Image_Logo.Source = null;
                }
                else {
                    TextBlock_Name.Text = result.Name;
                    TextBlock_Description.Text = result.Description;
                    Image_Logo.Source = new BitmapImage(new Uri(result.ImageUrl, UriKind.Absolute));
                }
            });
        }

        private async Task IdentifyUser(SoftwareBitmap frame) {

            var userService = _ServiceProvider.GetService<IUserService>();
            var user = await userService.IdentifyUserAsync(frame);

            // TODO: Show the user information here
            Trace.TraceInformation($"Setting name '{user.Name}'...");
        }

        private async void OnFaceDetectionStartedAsync(Object sender, EventArgs args) {

            _TimeoutTimer.Enabled = false;

            if (_PhotoTimer.Enabled) {
                return;
            }

            StartTimer();

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                TextBlock_Name.Text = "Searching for beers...";
                TextBlock_Description.Text = "";
                Image_Logo.Source = null;
            });
        }

        private void OnFacesNoLongerDetected(Object sender, EventArgs args) {

            if (_TimeoutTimer.Enabled) {
                return;
            }

            _TimeoutTimer.Start();
        }

        private async void NoOneIsPresent(Object sender, EventArgs args) {

            StopTimer();

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                TextBlock_Name.Text = "Try moving a little closer...";
                TextBlock_Description.Text = "";
                Image_Logo.Source = null;
            });
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e) {

            //cameraControl.FaceDetectionStarted += OnFaceDetectionStartedAsync;
            //cameraControl.FacesNoLongerDetected += OnFacesNoLongerDetected;

            //_PhotoTimer = new Timer(3000);
            //_PhotoTimer.Elapsed += OnTimerElapsed;
            //_PhotoTimer.AutoReset = true;
            //_PhotoTimer.Enabled = false;

            //_TimeoutTimer = new Timer(15000);
            //_TimeoutTimer.Elapsed += NoOneIsPresent;
            //_TimeoutTimer.Enabled = false;

            await cameraControl.StartStreamAsync(isForRealTimeProcessing: true);

            base.OnNavigatedTo(e);
        }
    }
}