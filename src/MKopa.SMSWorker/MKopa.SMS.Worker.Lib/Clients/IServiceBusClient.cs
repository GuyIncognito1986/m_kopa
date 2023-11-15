namespace MKopa.SMS.Worker.Lib.Clients;

public interface IServiceBusClient
{
    public Task PublishToDeadLetterAsync(byte[] payload);
    public Task PublishToMessageSendCompletedAsync(byte[] payload);
    public byte[] Serialize<T>(T message);
}