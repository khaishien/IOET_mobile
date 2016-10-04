using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.SpeechClient
{

    class ResetEnrollException : Exception
    {
        public ResetEnrollException(string message)
            : base(message)
        {
        }
    }
}
