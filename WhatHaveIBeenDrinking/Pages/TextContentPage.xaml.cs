using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Timers;
using WhatHaveIBeenDrinking.Entities;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WhatHaveIBeenDrinking.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TextContentPage : Page
    {
        private DispatcherTimer _ReturnTimer;
        private const int NAVIGATION_DURATION = 15;

        public TextContentPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var param = (Tuple<Drink, Content>)e.Parameter;

            var drink = param.Item1;
            var content = param.Item2;

            TextBlock_Title.Text = content.Title;
            TextBlock_Content.Text = content.Description;

            if (drink.ImageUrl != null)
            {
                Image_Logo.Source = new BitmapImage(new Uri(drink.ImageUrl));
            }

            if (content.IconType != null && drink.IconTypes.ContainsKey(content.IconType))
            {
                Image_Icon.Source = new BitmapImage(new Uri(drink.IconTypes[content.IconType]));
            }

            _ReturnTimer = new DispatcherTimer();
            _ReturnTimer.Interval = new TimeSpan(0, 0, NAVIGATION_DURATION);
            _ReturnTimer.Tick += GoToThankYou;
            _ReturnTimer.Start();
        }

        private void GoToThankYou(object sender, object e)
        {
            _ReturnTimer.Stop();
            Frame.Navigate(typeof(ThankYouPage));
        }
    }
}
