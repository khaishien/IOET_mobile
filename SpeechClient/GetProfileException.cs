using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaregiverMobile.SpeechClient
{
    public class GetProfileException : Exception
    {
        /// <summary>
        /// Constructor to create an exception with a specified message
        /// </summary>
        /// <param name="message">Exception message</param>
        public GetProfileException(string message)
            : base(message)
        {
        }
    }
}
