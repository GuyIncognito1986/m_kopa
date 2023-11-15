namespace MKopa.SMS.Worker.Lib.DomainModel;

public class FailedDeserializationDeadLetterMessage
{
    public string Message { get; }

    public FailedDeserializationDeadLetterMessage(string message)
    {
        Message = message;
    }
}