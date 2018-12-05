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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WhatHaveIBeenDrinking.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DrinkContentPage : Page
    {
        private DispatcherTimer _NavigationTimer;
        private const int NAVIGATION_DURATION = 10;

        public DrinkContentPage()
        {
            this.InitializeComponent();
        }

        private Content SelectRandomContentForDrink(Drink drink)
        {
            if (drink?.Content == null || drink.Content.Count == 0)
            {
                return null;
            }

            var random = new Random();
            var r = random.Next(drink.Content.Count);
            return drink.Content[r];
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var drink = (Drink)e.Parameter;

            // Set drink properties
            TextBlock_Name.Text = drink.Name;
            TextBlock_Description.Text = drink.Description;

            if (drink.BottleImageUrl != null)
            {
                Image_Bottle.Source = new BitmapImage(new Uri(drink.BottleImageUrl));
            }

            _NavigationTimer = new DispatcherTimer();
            _NavigationTimer.Interval = new TimeSpan(0, 0, NAVIGATION_DURATION);
            _NavigationTimer.Tick += ReturnToHome;

            // Determine next content source
            var content = SelectRandomContentForDrink(drink);

            if (content != null)
            {
                var param = new Tuple<Drink, Content>(drink, content);

                switch (content.Type)
                {
                    case "Video":
                        _NavigationTimer.Tick += (sender, args) => GoToVideoContent(param);
                        break;
                    case "Text":
                        _NavigationTimer.Tick += (sender, args) => GoToTextContent(param);
                        break;
                }
            }

            // Navigate to next content source after timer expiration
            _NavigationTimer.Start();
        }

        private void ReturnToHome(object sender, object e)
        {
            _NavigationTimer.Stop();
            Frame.GoBack();
        }

        private void GoToVideoContent(Tuple<Drink, Content> videoContent)
        {
            _NavigationTimer.Stop();
            Frame.Navigate(typeof(VideoContentPage), videoContent, new EntranceNavigationTransitionInfo());
        }

        private void GoToTextContent(Tuple<Drink, Content> textContent)
        {
            _NavigationTimer.Stop();
            Frame.Navigate(typeof(TextContentPage), textContent, new EntranceNavigationTransitionInfo());
        }
    }
}
