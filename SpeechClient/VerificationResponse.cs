using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.SpeechClient
{
    public enum VerificationResult
    {
        Accept,
        Reject
    }

    public enum VerificationConfidence
    {
        Low,
        Normal,
        High
    }

    public class VerificationResponse
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public VerificationResult result { get; set; }
        public VerificationConfidence confidence { get; set; }
        public string phrase { get; set; }

    }
}
