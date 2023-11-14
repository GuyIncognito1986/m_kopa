using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKopa.SMS.Worker.Lib.DomainModel
{
    public class DeadLetterMessage
    {
        public SendSmsCommand FailedCommand { get; }
        public string ErrorMessage { get; }

        public DeadLetterMessage(SendSmsCommand failedCommand, string errorMessage)
        {
            FailedCommand = failedCommand;
            ErrorMessage = errorMessage;
        }
    }
}
