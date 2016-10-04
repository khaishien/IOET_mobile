using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.SpeechClient
{
    public class VerificationProfile
    {
        public string verificationProfileId { get; set; }
        public string locale { get; set; }
        public int enrollmentsCount { get; set; }
        public int remainingEnrollmentsCount { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime lastActionDateTime { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public EnrollmentStatus EnrollmentStatus { get; set; }
    }
}
