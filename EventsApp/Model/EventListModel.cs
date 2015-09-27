using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace EventsApp.Model
{
    public class EventListModel :List<EventModel>
    {
         public EventListModel(JsonArray categoryArray) : base()
        {
            for (uint i = 0; i < categoryArray.Count; i++)
            {
                Add(new EventModel(categoryArray.GetObjectAt(i)));
            }
        }
    }
}
