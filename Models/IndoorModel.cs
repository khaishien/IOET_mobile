using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.Models
{
    public class IndoorModel
    {
        public ZoneModel zoneid { get; set; }
        public string tagid { get; set; }
        public DateTime timestamp { get; set; }
    }
}
