using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.Models
{
    class SendChatModel
    {
        public string to { get; set; }
        public string from { get; set; }
        public string content { get; set; }
        public DateTime timestamp { get; set; }

    }
}
