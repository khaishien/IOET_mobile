using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.SpeechClient
{
    class GetPhraseException : Exception
    {
        public GetPhraseException(string message)
            : base(message)
        {
        }
    }
}
