namespace MKopa.SMS.Worker.Lib.DomainModel;

using Visus.Cuid;

public class MessageSent
{
    public Cuid2 CorrelationId { get; }

    public MessageSent(Cuid2 correlationId)
    {
        CorrelationId = correlationId;
    }
}