using EventsApp.Common;
using EventsApp.Model;
using EventsApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Security.Credentials;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace EventsApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EventDetailPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private EventModel eve;
        private PasswordVault vault;

        public EventDetailPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            this.Loaded += EventDetailPage_Loaded;
        }

        void EventDetailPage_Loaded(object sender, RoutedEventArgs e)
        {
            eventTitleBlock.Width = LayoutRoot.ActualWidth - 40;
            userNameBlock.Text = StaticData.user.name;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (StaticData.user.name == "")
            {
                favouriteButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            RequestConnector request = new RequestConnector("/events/"+(int)e.NavigationParameter,true,null);
            request.ResponseReceived += eventDetailsRequest_ResponseReceived;

            
        }

        async void eventDetailsRequest_ResponseReceived(object sender, RequestConnector.ResponseReceivedEventArgs e)
        {
            eve = new EventModel(JsonObject.Parse(e.Content));
            eventImage.DataContext = eve;
            eventTitleBlock.Text = eve.eventName;
            eventDateBlock.Text = eve.eventDate + " " + eve.eventHours;
            eventTypeBlock.Text = eve.eventType;
            eventUrl.Content = eve.eventUrl;
            eventDescriptionBlock.Text = eve.description;
            if (eve.location.name != "")
            {
                eventLocationName.Text = eve.location.name;
            }
            else
            {
                eventLocationName.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                locationNameBlock.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            if (eve.location.address != "")
            {
                eventAddressBlock.Text = eve.location.address;
            }
            else
            {
                eventAddressBlock.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                addresTxtBlock.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            if (eve.location.isVisible)
            {
                setMapPosition();
            }
            else
            {
                eventMap.Height = 0;
                locBlock.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

            if (!eve.hasPhoto)
                eventImage.MaxHeight = 100;

            HttpRequestMessage mg = new HttpRequestMessage();
            mg.Method = HttpMethod.Get;
            mg.Headers.Add("Authorization", "Basic " + StaticData.user.token);

            RequestConnector connector = new RequestConnector("/events/favourites",mg);

            connector.ResponseReceived += favConnector_ResponseReceived;

            await Task.Delay(500);
            myProgressRing.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            ContentRoot.Height = double.NaN;
           
        }

        void favConnector_ResponseReceived(object sender, RequestConnector.ResponseReceivedEventArgs e)
        {
            JsonArray ja = JsonArray.Parse(e.Content);
            for (uint i = 0; i < ja.Count; i++)
            {
                if ((int)ja.GetNumberAt(i) == eve.eventID)
                {
                    favouriteButton.IsChecked = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void setMapPosition()
        {
            eventMap.Center = new Windows.Devices.Geolocation.Geopoint(new Windows.Devices.Geolocation.BasicGeoposition() { Latitude = eve.location.lat, Longitude = eve.location.lon });
            eventMap.ZoomLevel = 18;
            MapIcon mapIcon = new MapIcon();
            mapIcon.Visible = true;
            mapIcon.ZIndex = 1000;
            mapIcon.Location = new Windows.Devices.Geolocation.Geopoint(new Windows.Devices.Geolocation.BasicGeoposition() { Latitude = eve.location.lat, Longitude = eve.location.lon });
            mapIcon.Title = "Event is here :D";
            mapIcon.NormalizedAnchorPoint = new Point(0.25, 0.9);
            eventMap.MapElements.Add(mapIcon);
        }

        private async void eventUrl_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(eve.eventUrl));
        }

        void favouriteConnector_ResponseReceived(object sender, RequestConnector.ResponseReceivedEventArgs e)
        {
            JsonObject o = JsonObject.Parse(e.Content);

            if ((int)o.GetNamedNumber("status") == 1)
                favouriteButton.IsChecked = true;
            else
                favouriteButton.IsChecked = false;
        }

        private void favouriteButton_Click(object sender, RoutedEventArgs e)
        {
            HttpRequestMessage m = new HttpRequestMessage();

            m.Method = HttpMethod.Post;
            m.Headers.Add("Authorization", "Basic " + StaticData.user.token);

            RequestConnector connector = new RequestConnector("/events/favourites/" + eve.eventID, m);
            connector.ResponseReceived += favouriteConnector_ResponseReceived;
        }

        private void logOutBtn_Click(object sender, RoutedEventArgs e)
        {
            vault = StaticData.vault;

            if (vault != null)
                vault.Remove(vault.Retrieve("EventsApp Credentials", StaticData.user.email));

            Frame.Navigate(typeof(MainPage));
        }
    }
}
