using EventsApp.Common;
using EventsApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using CryptSharp;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI.Popups;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace EventsApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RegisterPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private MessageDialog md;

        public RegisterPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            //getDeviceID();
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

        private void registerUser()
        {
            string passwordHashed = HashMsg(HashAlgorithmNames.Sha256, passwordBox.Password.ToString());

            RequestConnector connector = new RequestConnector("/auth/signup",false,new System.Net.Http.FormUrlEncodedContent(new[]{
                new KeyValuePair<string,string>("name",nameBox.Text +" "+lastNameBox.Text),
                new KeyValuePair<string,string>("email",mailBox.Text),
                new KeyValuePair<string,string>("passwd",passwordHashed),
                new KeyValuePair<string,string>("device_id",getDeviceID()),
                new KeyValuePair<string,string>("device_type","0")
            }));

            connector.ResponseReceived += connector_ResponseReceived;
            }

        async void connector_ResponseReceived(object sender, RequestConnector.ResponseReceivedEventArgs e)
        {
            md = new MessageDialog("","");
            switch (e.Message.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    md.Title = "Info";
                    md.Content = "Registration complete";
                    await md.ShowAsync();
                    passwordBox.Password = "";
                    rePasswordBox.Password = "";
                    mailBox.Text = "";
                    break;
                case System.Net.HttpStatusCode.BadRequest:
                    md.Title = "Error";
                    md.Content = "You can't have only empty fields";
                    await md.ShowAsync();
                    break;
                case System.Net.HttpStatusCode.Conflict:
                    md.Title = "Warning";
                    md.Content = "This email is in use";
                    passwordBox.Password = "";
                    rePasswordBox.Password = "";
                    await md.ShowAsync();
                    break;
            }
        }
        private string getDeviceID()
        {
            HardwareToken myToken = HardwareIdentification.GetPackageSpecificToken(null);
            IBuffer hardwareId = myToken.Id;
            byte[] hwidBytes = hardwareId.ToArray();

            string deviceID = hwidBytes.Select(b => b.ToString()).Aggregate((b, next) => b + next);

            string encodedDeviceID = HashMsg(HashAlgorithmNames.Sha256, deviceID);

            return encodedDeviceID;
        }

        private void testCrypt(object sender, TappedRoutedEventArgs e)
        {
            //getDeviceType();
            registerUser();
            
        }
        public String HashMsg(String strAlgName, String strMsg)
        {
            IBuffer buffUtf8Msg = CryptographicBuffer.ConvertStringToBinary(strMsg, BinaryStringEncoding.Utf8);

            HashAlgorithmProvider objAlgProv = HashAlgorithmProvider.OpenAlgorithm(strAlgName);

            String strAlgNameUsed = objAlgProv.AlgorithmName;

            IBuffer buffHash = objAlgProv.HashData(buffUtf8Msg);

            if (buffHash.Length != objAlgProv.HashLength)
            {
                throw new Exception("There was an error creating the hash");
            }

            String strHashBase64 = CryptographicBuffer.EncodeToHexString(buffHash);

            return strHashBase64;

        }

        private void getDeviceType()
        {
            try
            {
                Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation deviceInfo = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
                var i = deviceInfo.Id;
            }
            catch (NotImplementedException ex)
            {
               
            }
        }

        private void denyButton_Click(object sender, RoutedEventArgs e)
        {
            nameBox.Text = "";
            passwordBox.Password = "";
            rePasswordBox.Password = "";
            mailBox.Text = "";

            Frame.Navigate(typeof(MainPage));
        }
    }

}
