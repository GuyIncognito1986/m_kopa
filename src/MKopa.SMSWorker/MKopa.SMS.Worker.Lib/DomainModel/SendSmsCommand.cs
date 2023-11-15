using MKopa.SMS.Worker.Lib.Exceptions;
using PhoneNumbers;
using Visus.Cuid;

namespace MKopa.SMS.Worker.Lib.DomainModel
{
    public class SendSmsCommand
    {
        public const string TextDefaultError = "SMS text can not be null!";
        public const string PhoneNumberDefaultError = "Phone number can not be null!";
        public const string CorrelationIdNotSetError = "Correlation id must be set!";
        public const string SMSTextTooLongError = "SMS text too long!";
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
            var listOfValidationErrors = new List<string>();
            if (Text == default) listOfValidationErrors.Add(TextDefaultError);
            if (PhoneNumber == default) listOfValidationErrors.Add(PhoneNumberDefaultError);
            if (CorrelationId == default) listOfValidationErrors.Add(CorrelationIdNotSetError);
            if (Text?.Length > 160) listOfValidationErrors.Add(SMSTextTooLongError);
            if (listOfValidationErrors.Any()) throw new SendSmsCommandValidationException(listOfValidationErrors);
        }

        public ThirdPartySmsMessage ToThirdPartySmsMessage()
        {
            return new ThirdPartySmsMessage(PhoneNumber, Text);
        }
    }
}
