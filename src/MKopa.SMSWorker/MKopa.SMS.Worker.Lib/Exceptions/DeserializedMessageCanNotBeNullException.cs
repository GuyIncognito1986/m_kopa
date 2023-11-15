using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKopa.SMS.Worker.Lib.Exceptions
{
    public class DeserializedMessageCanNotBeNullException: Exception
    {
        public DeserializedMessageCanNotBeNullException() : base("Something is really wrong in the sms state machine at runtime, deserialized message should not be null at this point!") 
        { 
        }
    }
}
