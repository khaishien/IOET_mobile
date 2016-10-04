using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.Models
{
    public class UserModel
    {
        public string api { get; set; }
        public string _id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string userfullname { get; set; }
        public string useremail { get; set; }
        public Uri userprofilepic { get; set; }
        public string usercontact { get; set; }
        public string useraddress { get; set; }
        public string userpostcode { get; set; }
        public string userstate { get; set; }
        public string usercountry { get; set; }
        public string userHomeLat { get; set; }
        public string userHomeLng { get; set; }
        public string userrole { get; set; }
        public string userfaceid { get; set; }
        public string uservoiceid { get; set; }
        public string usernfcid { get; set; }
        public DateTime login_at { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public bool alert { get; set; }
        public string alertstring { get; set; }
        public bool active { get; set; }
        public string activestring { get; set; }
        public bool statusmonitor { get; set; }


    }
}
