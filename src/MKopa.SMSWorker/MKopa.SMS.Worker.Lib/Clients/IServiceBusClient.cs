namespace MKopa.SMS.Worker.Lib.Clients;

public interface IServiceBusClient
{
    public void PublishToDeadLetterAsync(byte[] payload);
    public void PublishToMessageSendCompletedAsync(byte[] payload);
    public byte[] Serialize<T>(T message);
}