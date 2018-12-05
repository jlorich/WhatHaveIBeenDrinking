using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WhatHaveIBeenDrinking.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ThankYouPage : Page
    {
        private DispatcherTimer _NavigationTimer;
        private const int NAVIGATION_DURATION = 5;

        public ThankYouPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _NavigationTimer = new DispatcherTimer();
            _NavigationTimer.Interval = new TimeSpan(0, 0, NAVIGATION_DURATION);
            _NavigationTimer.Tick += GoToMainPage;
            _NavigationTimer.Start();
        }

        private void GoToMainPage(object sender, object e)
        {
            _NavigationTimer.Stop();
            Frame.Navigate(typeof(MainPage));
        }
    }
}
