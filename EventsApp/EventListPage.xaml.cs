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
using Windows.Phone.UI.Input;
using Windows.Security.Credentials;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
    public sealed partial class EventListPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private EventListModel events;
        private PasswordVault vault;
        private RelayCommand goBack;

        public EventListPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            goBack = new RelayCommand(() => this.HardwareButtons_BackPressed(),() => this.CanCheckGoBack());

            this.navigationHelper.GoBackCommand = goBack;


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
            setUserData();
            loadEvents();

            if (StaticData.user.name == "")
            {
                onlyFavBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                changePasswordAppBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
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

        private void setUserData()
        {
            if (StaticData.user.name!= "")
            {
                userNameBlock.Text = "Welcome " + StaticData.user.name;
            }
            else
            {
                userNameBlock.Text = "Welcome Anonymous";
            }
        }

        private void loadEvents()
        {
            RequestConnector request = new RequestConnector("/events?timestamp=0",true,null);

            request.ResponseReceived += eventRequest_ResponseReceived;
        }

        async void eventRequest_ResponseReceived(object sender, RequestConnector.ResponseReceivedEventArgs e)
        {
            JsonArray jaEvents = JsonArray.Parse(e.Content);
            EventListModel eventModel = new EventListModel(jaEvents);

            events = eventModel;
            eventsListView.ItemsSource = eventModel;

            await Task.Delay(500);
            myProgressRing.Visibility = Visibility.Collapsed;
            eventsListView.Height = double.NaN;
        }

        private void getMoreInfoAboutEvent(object sender, SelectionChangedEventArgs e)
        {


            Frame.Navigate(typeof(EventDetailPage), ((EventModel)eventsListView.SelectedItem).eventID);
        }

        private void getOnlyFavourites()
        {
            eventsListView.Height = 0;
            myProgressRing.Visibility = Visibility.Visible;

            HttpRequestMessage mg = new HttpRequestMessage();
            mg.Method = HttpMethod.Get;
            mg.Headers.Add("Authorization","Basic "+StaticData.user.token);

            RequestConnector connector = new RequestConnector("/events/favourites", mg);
            connector.ResponseReceived += favConnector_ResponseReceived;
        }

        async void favConnector_ResponseReceived(object sender, RequestConnector.ResponseReceivedEventArgs e)
        {
            JsonArray ja = JsonArray.Parse(e.Content);

            List<EventModel> favEventModel = new List<EventModel>();

            for (int i = 0; i < events.Count; i++)
            {
                for (uint j = 0; j < ja.Count; j++)
                {
                    if (events[i].eventID == (int)ja.GetNumberAt(j))
                    {
                        favEventModel.Add(events[i]);
                    }
                }
            }

            eventsListView.ItemsSource = favEventModel;

            await Task.Delay(500);
            myProgressRing.Visibility = Visibility.Collapsed;
            eventsListView.Height = double.NaN;
        }

        private void AppBarToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            getOnlyFavourites();
            
        }

        private async void AppBarToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            eventsListView.Height = 0;
            myProgressRing.Visibility = Visibility.Visible;

            eventsListView.ItemsSource = events;

            await Task.Delay(500);
            myProgressRing.Visibility = Visibility.Collapsed;
            eventsListView.Height = double.NaN;
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            vault = StaticData.vault;

            try
            {
                if (vault != null)
                    vault.Remove(vault.Retrieve("EventsApp Credentials", StaticData.user.email));
            }
            catch (Exception ex)
            {

            }

            Frame.Navigate(typeof(MainPage));

        }

        private async void HardwareButtons_BackPressed()
        {
            MessageDialog dlg = new MessageDialog("Are you sure you want to quit ?", "Warning");
            dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(CommandHandler1)));
            dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler(CommandHandler1)));

            await dlg.ShowAsync();
        }

        private void CommandHandler1(IUICommand command)
        {
            var label = command.Label;
            switch (label)
            {
                case "Yes":
                    {
                        Application.Current.Exit();
                        break;
                    }
                case "No":
                    {
                        break;
                    }
            }
        }

        private bool CanCheckGoBack()
        {
            return true;
        }

        private void changePasswordAppBtn_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ResetPasswordPage));
        }
    }
}
