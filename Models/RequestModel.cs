using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.Models
{
    class RequestModel
    {
        public string _id { get; set; }
        public UserModel caregiverid { get; set; }
        public UserModel elderlyid { get; set; }
        public string relationship { get; set; }
        public Boolean? requeststatus { get; set; }
        public string statusString { get; set; }

    }
}
