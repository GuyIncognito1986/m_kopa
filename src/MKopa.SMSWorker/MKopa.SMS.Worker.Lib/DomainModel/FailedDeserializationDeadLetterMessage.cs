namespace MKopa.SMS.Worker.Lib.DomainModel;

public class FailedDeserializationDeadLetterMessage
{
    public byte[] Message { get; }

    public FailedDeserializationDeadLetterMessage(byte[] message)
    {
        Message = message;
    }
}