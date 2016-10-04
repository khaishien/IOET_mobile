using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.SpeechClient
{
    internal class VerificationOperationResponse
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationStatus Status { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastActionDateTime { get; set; }
        public string Message { get; set; }
        public VerificationResponse ProcessingResult { get; set; }
    }
}
