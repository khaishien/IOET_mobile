using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.SpeechClient
{
    public enum EnrollmentStatus
    {
        Enrolling,
        Training,
        Enrolled
    }

    public class EnrollmentResponse
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public EnrollmentStatus EnrollmentStatus { get; set; }
        public int enrollmentsCount { get; set; }
        public int remainingEnrollments { get; set; }
        public string phrase { get; set; }
    }
}
