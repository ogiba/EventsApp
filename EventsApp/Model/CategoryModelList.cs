using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace EventsApp.Model
{
    public class CategoryModelList : List<string>
    {
        public CategoryModelList(JsonArray categoryArray) : base()
        {
            for (uint i = 0; i < categoryArray.Count; i++)
            {
                Add(categoryArray.GetStringAt(i));
            }
        }
    }
}
