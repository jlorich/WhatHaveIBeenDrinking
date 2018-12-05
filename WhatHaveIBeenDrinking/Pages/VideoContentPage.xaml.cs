using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using WhatHaveIBeenDrinking.Entities;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
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
    public sealed partial class VideoContentPage : Page
    {
        public VideoContentPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {   
            var param = (Tuple<Drink, Content>)e.Parameter;

            var drink = param.Item1;
            var content = param.Item2;

            TextBlock_Description.Text = content.Title;
            if (drink.ImageUrl != null)
            {
                Image_Logo.Source = new BitmapImage(new Uri(drink.ImageUrl));
            }
            
            MediaElement_Player.Source = new Uri(content.Url);
            MediaElement_Player.Play();
            MediaElement_Player.MediaEnded += GoToThankYou;
        }

        private void GoToThankYou(object sender, object e)
        {
            Frame.Navigate(typeof(ThankYouPage));
        }
    }
}
