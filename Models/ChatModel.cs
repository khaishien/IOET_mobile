using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.Models
{
    class ChatModel
    {
        public UserModel to { get; set; }
        public UserModel from { get; set; }
        public string content { get; set; }
        public DateTime timestamp { get; set; }
        public string alignment { get; set; }

    }
}
