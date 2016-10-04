using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.SpeechClient
{
    class VerificationException : Exception
    {
        public VerificationException(string message)
            : base(message)
        {
        }
    }

}
