namespace MKopa.SMS.Worker.Lib.Clients;

using DomainModel;

public interface IQueueClient
{
    public Task<IAsyncEnumerable<byte[]>> SubscribeToSmsCommandsAsync();
    public SendSmsCommand Deserialize(byte[] command);
}