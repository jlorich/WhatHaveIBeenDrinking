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
using WhatHaveIBeenDrinking.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WhatHaveIBeenDrinking.Repositories;
using WhatHaveIBeenDrinking.Services;
using Windows.UI.Xaml.Media.Imaging;

namespace WhatHaveIBeenDrinking
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string SETTINGS_FILE_LOCATION = "appsettings.json";

        private static IConfiguration _Configuration;

        private static IServiceCollection _Services;

        private static IServiceProvider _ServiceProvider;

        public MainPage()
        {
            InitializeComponent();
            BuildConfiguration();
            ConfigureServices();
        }
        
        public static void ConfigureServices()
        {
            _Services = new ServiceCollection();

            _Services.AddOptions();
            _Services.Configure<KioskOptions>(_Configuration);
            _Services.AddTransient<KioskRepository>();
            _Services.AddTransient<ImageClassificationService>();
            _Services.AddTransient<KioskService>();

            _ServiceProvider = _Services.BuildServiceProvider();
        }

        public static void BuildConfiguration()
        {
            var packageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            _Configuration = new ConfigurationBuilder()
                .SetBasePath(packageFolder.Path.ToString())
                .AddJsonFile(SETTINGS_FILE_LOCATION, optional: true, reloadOnChange: true)
                .Build();
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

        private async void Button_camera_Click(object sender, RoutedEventArgs e)
        {
            var frame = await cameraControl.GetFrame();

            if (frame == null)
            {
                return;
            }

            var kioskService = _ServiceProvider.GetService<KioskService>();

            var result = await kioskService.IdentifyDrink(frame);

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (string.IsNullOrEmpty(result.Name))
                {
                    TextBlock_Name.Text = "No drinks detected";
                    TextBlock_Description.Text = "";
                    Image_Logo.Source = null;
                }
                else
                {
                    TextBlock_Name.Text = result.Name;
                    TextBlock_Description.Text = result.Description;
                    Image_Logo.Source = new BitmapImage(new Uri(result.ImageUrl, UriKind.Absolute));
                }
            });
        }

        private void OnFaceDetectedAsync(object sender, FaceDetectedEventArgs args)
        {
            Console.Out.WriteLine("Face detected");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //cameraControl.FaceDetected += OnFaceDetectedAsync;

            await cameraControl.StartStreamAsync(isForRealTimeProcessing: true);

            base.OnNavigatedTo(e);
        }
    }
}
