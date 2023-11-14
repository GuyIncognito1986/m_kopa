using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKopa.SMS.Worker.Lib.Exceptions
{
    public class SendSmsCommandValidationException : Exception
    {
        public SendSmsCommandValidationException(IList<string> validationMessages): base(validationMessages.Aggregate(string.Empty, (m, a) => $"{a} {m}").TrimEnd(' '))
        {
        }
    }
}
