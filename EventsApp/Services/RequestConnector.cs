using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace EventsApp.Services
{
    class RequestConnector
    {
        public event EventHandler<ResponseReceivedEventArgs> ResponseReceived;

        public RequestConnector(string apiUri,bool isGet, HttpContent content)
        {
            getData(apiUri, isGet, content);
        }

        public RequestConnector(string apiUri, HttpRequestMessage m)
        {
            getData(apiUri, m);
        }

        private async void getData(string apiUri)
        {
            getData(apiUri, true, null);
        }

        private async void getData(string apiUri, bool isGet, HttpContent content)
        {
            using (var client = new HttpClient())
            {
                if(!isGet){
                    var result = await client.PostAsync("http://posnania.test.appchance.com/api" + apiUri, content);
                    checkResult(result);

                }else{
                    var result = await client.GetAsync("http://posnania.test.appchance.com/api" + apiUri);
                    checkResult(result);

                }
            }
        }

        private async void getData(string apiUri, HttpRequestMessage message)
        {
            using (var client = new HttpClient())
            {
                message.RequestUri = new Uri("http://posnania.test.appchance.com/api" + apiUri);
                var result = await client.SendAsync(message);
                checkResult(result);
            }
        }

        private async void checkResult(HttpResponseMessage result)
        {

            if (ResponseReceived != null)
                ResponseReceived(this, new ResponseReceivedEventArgs(result.Content.ReadAsStringAsync().Result, result));
        }

        public class ResponseReceivedEventArgs : EventArgs
        {
            private string content;
            private HttpResponseMessage message;

            public ResponseReceivedEventArgs(string content, HttpResponseMessage message)
            {
                this.content = content;
                this.message = message;
            }

            public string Content { get { return content; } }
            public HttpResponseMessage Message { get { return message; } }
        }
    }
}
