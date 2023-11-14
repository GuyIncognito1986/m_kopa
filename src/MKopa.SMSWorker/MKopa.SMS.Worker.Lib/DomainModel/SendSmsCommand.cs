using PhoneNumbers;
using Visus.Cuid;

namespace MKopa.SMS.Worker.Lib.DomainModel
{
    public class SendSmsCommand
    {
        public Cuid2 CorrelationId { get; }
        public PhoneNumber PhoneNumber { get; }
        public string Text { get; }

        public SendSmsCommand(Cuid2 correlationId, PhoneNumber phoneNumber, string text)
        { 
            PhoneNumber = phoneNumber;
            CorrelationId = correlationId;
            Text = text;
        }

        public void Validate()
        {
            if (Text == null) throw new ArgumentNullException("SMS text can not be null!");
            if (PhoneNumber == null) throw new ArgumentNullException("Phone number can not be null!");
            if (CorrelationId == default) throw new ArgumentException("Correlation id must be set!");
            if (Text.Length > 160) throw new ArgumentException("SMS text too long!");
        }
    }
}
