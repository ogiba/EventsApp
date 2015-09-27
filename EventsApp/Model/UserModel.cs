using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace EventsApp.Model
{
    public class UserModel
    {
        public int id { get; set; }

        
        public string token { get; set; }

        
        public string name { get; set; }
        
        public string email { get; set; }
        
        public int push { get; set; }

        
        public CategoryModelList category { get; set; }
        
        
        public int anonymous { get; set; }

        public UserModel(JsonObject userObject)
        {
            this.id = (int)userObject.GetNamedNumber("id");
            this.token = userObject.GetNamedString("token");
            this.name = userObject.GetNamedString("name");
            this.email = userObject.GetNamedString("email");
            this.push = (int)userObject.GetNamedNumber("push");
            this.category = new CategoryModelList(userObject.GetNamedArray("categories"));
            this.anonymous = (int)userObject.GetNamedNumber("anonymous");
        }
    }
}
