using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Xaml;

namespace EventsApp.Model
{
    public class EventModel
    {
        public int eventID { get; set; }
        public string eventName { get; set; }
        public string eventType { get; set; }
        public string eventDate { get; set; }
        public string eventHours { get; set; }
        public Location location { get; set; }
        public string eventUrl { get; set; }
        public string photo { get; set; }

        public string description { get; set; }
        public bool promoted { get; set; }
        public bool hasPhoto { get; set; }
        public EventModel(JsonObject eventObject)
        {
            this.eventID = (int)eventObject.GetNamedNumber("id") ;
            this.eventName = eventObject.GetNamedString("name");
            this.eventType = eventObject.GetNamedString("type");
            this.eventDate = eventObject.GetNamedString("date");
            this.eventHours = eventObject.GetNamedString("hours");
            this.location = new Location(eventObject.GetNamedObject("location"));
            this.eventUrl = eventObject.GetNamedString("url");
            this.photo = eventObject.GetNamedString("photo");

            if (string.IsNullOrWhiteSpace(this.photo))
            {
                this.photo = "Assets/b_no_image_icon.png";
                this.hasPhoto = false;
            }
            else
            {
                this.hasPhoto = true;
            }

            this.promoted = eventObject.GetNamedBoolean("promoted");

            if (eventObject.ContainsKey("description"))
            {
                this.description = eventObject.GetNamedString("description");
            }

        }
    }

    public class Location
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public bool isVisible { get; set; }

        public Location(JsonObject locationObject)
        {
            this.lat = locationObject.GetNamedNumber("lat");
            this.lon = locationObject.GetNamedNumber("lon");
            this.name = locationObject.GetNamedString("name");
            this.address = locationObject.GetNamedString("address");

            this.isVisible = lat != 0 && lon != 0;
        }
    }
}
