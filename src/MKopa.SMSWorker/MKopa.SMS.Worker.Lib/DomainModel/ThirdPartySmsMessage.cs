namespace MKopa.SMS.Worker.Lib.DomainModel;

public class ThirdPartySmsMessage
{
    public string PhoneNumber { get; }
    public string Text { get; }
    
    public ThirdPartySmsMessage(string phoneNumber, string text)
    {
        PhoneNumber = phoneNumber;
        Text = text;
    }
}