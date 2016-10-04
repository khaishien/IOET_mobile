using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.Models
{
    class GooglePlaceIdModel
    {
        public string placeid { get; set; }
        public string placeaddress { get; set; }
        public List<OutdoorModel> outdoorModels { get; set; } 
        public int visit_count { get; set; }
    }
}
