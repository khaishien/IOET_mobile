using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.Models
{
    class CaregiverMonitorModel
    {
        public List<UserModel> users { get; set; }
        public List<RequestModel> requests { get; set; }
        public List<TrackListModel> tracklists { get; set; }
    }
}
