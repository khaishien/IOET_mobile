using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.Models
{
    
    public class AccessPoint
    {
        public Location location { get; set; }
        public List<string> travel_modes { get; set; }
    }

    public class Location2
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }
    

    public class GoogleMapPlaceModel
    {
        public List<object> html_attributions { get; set; }
        public Result result { get; set; }
        public string status { get; set; }
        public int count { get; set; }

    }
}
