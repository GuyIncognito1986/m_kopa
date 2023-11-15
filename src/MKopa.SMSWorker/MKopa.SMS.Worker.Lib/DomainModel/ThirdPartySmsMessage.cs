using PhoneNumbers;

namespace MKopa.SMS.Worker.Lib.DomainModel;

public class ThirdPartySmsMessage
{
    public PhoneNumber PhoneNumber { get; }
    public string Text { get; }
    
    public ThirdPartySmsMessage(PhoneNumber phoneNumber, string text)
    {
        PhoneNumber = phoneNumber;
        Text = text;
    }
}