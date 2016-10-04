using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.Models
{
    public class TrackListModel
    {
        public string _id { get; set; }
        public UserModel caregiverid { get; set; }
        public UserModel elderlyid { get; set; }
        public string relationship { get; set; }
        public IndoorModel indoor { get; set; }
        public OutdoorModel outdoor { get; set; }
        public string status { get; set; }
        public string statuscolor { get; set; }


    }
    
}
