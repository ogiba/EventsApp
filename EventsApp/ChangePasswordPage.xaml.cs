using EventsApp.Common;
using EventsApp.Model;
using EventsApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
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
    public sealed partial class ResetPasswordPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private MessageDialog md;
        private PasswordVault vault;
        private const string VAULT_RESOURCE = "EventsApp Credentials";
        private string hashedNewPw;

        public ResetPasswordPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
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
            userNameBlock.Text = StaticData.user.name;
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

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            HttpRequestMessage mg = new HttpRequestMessage();
            mg.Method = HttpMethod.Post;
            mg.Headers.Add("Authorization", "Basic " + StaticData.user.token);

            if(newPasswordBox.Password == reNewPasswordBox.Password){
                string hashedOldPw = hashMsg(HashAlgorithmNames.Sha256, oldPasswordBox.Password.ToString());
                hashedNewPw = hashMsg(HashAlgorithmNames.Sha256, newPasswordBox.Password.ToString());

                mg.Content = new FormUrlEncodedContent(new[]{
                    new KeyValuePair<string,string>("old",hashedOldPw),
                    new KeyValuePair<string,string>("new",hashedNewPw)
                });

                RequestConnector request = new RequestConnector("/settings/passwd", mg);
                request.ResponseReceived += request_ResponseReceived;
            }
            
            
        }

        async void request_ResponseReceived(object sender, RequestConnector.ResponseReceivedEventArgs e)
        {
            vault = StaticData.vault;

            md = new MessageDialog("","");
            switch (e.Message.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    md.Title = "Passsword Change";
                    md.Content="Password has been changed";
                    await md.ShowAsync();

                    if (vault != null)
                    {
                        vault.Remove(vault.Retrieve(VAULT_RESOURCE, StaticData.user.email));
                        vault.Add(new PasswordCredential(VAULT_RESOURCE, StaticData.user.email, hashedNewPw));
                    }

                    Frame.Navigate(typeof(EventListPage));
                    
                    break;
                case System.Net.HttpStatusCode.BadRequest:
                    md.Title = "Password Change";
                    md.Content = "You have empty fields";
                    await md.ShowAsync();
                    break;
                case System.Net.HttpStatusCode.Unauthorized:
                    md.Title = "Password Change Error";
                    md.Content = "Old password is wrong";
                    await md.ShowAsync();
                    break;
            }
        }

        public String hashMsg(String strAlgName, String strMsg)
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

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(EventListPage));
        }
    }
}
