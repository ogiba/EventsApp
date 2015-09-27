using EventsApp.Model;
using EventsApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace EventsApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private PasswordVault vault = new PasswordVault();
        private const string VAULT_RESOURCE = "EventsApp Credentials";
        private string userName;
        private string password;
        private bool isLoaded = false;
        public MainPage()
        {
            //if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("FirstLaunch"))
            //{
            //    vault.Add(new PasswordCredential(VAULT_RESOURCE, "", ""));
            //    Windows.Storage.ApplicationData.Current.LocalSettings.Values["FirstLaunch"] = "Launched";

            //}
            //else {
                try
                {
                    var creds = vault.FindAllByResource(VAULT_RESOURCE).FirstOrDefault();
                    if (creds != null)
                    {
                        isLoaded = true;
                        userName = creds.UserName;
                        password = vault.Retrieve(VAULT_RESOURCE, userName).Password;

                        StaticData.vault = vault;

                        loginUser(userName, password);
                    }
                    
                }
                catch (Exception ex)
                {
                    // this exception likely means that no credentials have been stored
                    
                }
            //}

            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            if (!isLoaded)
            {
                myProgressRing.Visibility = Visibility.Collapsed;
                mainGrid.Height = double.NaN;
            }

        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            rememberUserBox.IsChecked = false;

            try
            {
                var creds = vault.FindAllByResource(VAULT_RESOURCE).FirstOrDefault();
                if (creds != null)
                {
                    isLoaded = true;
                    userName = creds.UserName;
                    password = vault.Retrieve(VAULT_RESOURCE, userName).Password;

                    StaticData.vault = vault;

                    loginUser(userName, password);
                }

            }
            catch (Exception ex)
            {
                // this exception likely means that no credentials have been stored

            }

            if (!isLoaded)
            {
                myProgressRing.Visibility = Visibility.Collapsed;
                mainGrid.Height = double.NaN;
            }

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private void registerUser(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RegisterPage));
            //EventsRequestor reqestor = new EventsRequestor("/events?timestamp=0",true,null);
        }

        private void loginUser(String login, string password)
        {
            RequestConnector request = new RequestConnector("/auth/signin", false, new System.Net.Http.FormUrlEncodedContent(new[]{
                new KeyValuePair<string,string>("email",login),
                new KeyValuePair<string,string>("passwd",password),
                new KeyValuePair<string,string>("deviceID",getDeviceID()),
                new KeyValuePair<string,string>("device_type","0")
            }));

            request.ResponseReceived += LoginRequest_ResponseReceived;

        }

        private async void LoginRequest_ResponseReceived(object sender, RequestConnector.ResponseReceivedEventArgs e)
        {
            MessageDialog mg = new MessageDialog("", "");
            switch (e.Message.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    JsonObject joUser = JsonObject.Parse(e.Content);
                    StaticData.user = new UserModel(joUser);

                    if (rememberUserBox.IsChecked.Value)
                    {
                        saveInfoAboutLogin();
                        StaticData.vault = vault;
                    }

                    Frame.Navigate(typeof(EventListPage));
                    break;
                case System.Net.HttpStatusCode.NotFound:
                    mg.Title = "Not found";
                    mg.Content = "wrong password or email";
                    await mg.ShowAsync();
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

        private string getDeviceID()
        {
            HardwareToken myToken = HardwareIdentification.GetPackageSpecificToken(null);
            IBuffer hardwareId = myToken.Id;
            byte[] hwidBytes = hardwareId.ToArray();

            string deviceID = hwidBytes.Select(b => b.ToString()).Aggregate((b, next) => b + next);

            string encodedDeviceID = hashMsg(HashAlgorithmNames.Sha256, deviceID);

            return encodedDeviceID;
        }

        private void saveInfoAboutLogin()
        {
          string hashedPw = hashMsg(HashAlgorithmNames.Sha256, passwordBox.Password.ToString());
          vault.Add(new PasswordCredential(VAULT_RESOURCE, StaticData.user.email, hashedPw));
        }

        private void loginToApp(object sender, RoutedEventArgs e)
        {
            string hashedPw = hashMsg(HashAlgorithmNames.Sha256, passwordBox.Password.ToString());
            loginUser(loginBox.Text, hashedPw);
        }

        private void forgotPasswordButn_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(GetNewPasswordPage));
        }

        private void enterWithoutLoginBtn_Click(object sender, RoutedEventArgs e)
        {
            HttpRequestMessage mg = new HttpRequestMessage();
            mg.Method = HttpMethod.Post;
            mg.Content = new FormUrlEncodedContent(new[]{
                new KeyValuePair<string,string>("device_id",getDeviceID()),
                new KeyValuePair<string,string>("device_type","0")
            });

            RequestConnector connector = new RequestConnector("/auth/anonymous", mg);
            connector.ResponseReceived += connector_ResponseReceived;
        }

        void connector_ResponseReceived(object sender, RequestConnector.ResponseReceivedEventArgs e)
        {
            switch (e.Message.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    JsonObject joUser = JsonObject.Parse(e.Content);
                    StaticData.user = new UserModel(joUser);
                    Frame.Navigate(typeof(EventListPage));
                    break;
                case System.Net.HttpStatusCode.BadRequest:
                    break;
            }
        }


    }
}
