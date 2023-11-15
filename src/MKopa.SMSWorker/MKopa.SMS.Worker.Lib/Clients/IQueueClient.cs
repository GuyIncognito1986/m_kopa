using MKopa.SMS.Worker.Lib.DomainModel;

namespace MKopa.SMS.Worker.Lib.Clients;

public interface IQueueClient
{
    public Task<IAsyncEnumerable<byte[]>> SubscribeToSmsCommands();
    public Task<SendSmsCommand> Deserialize(byte[] message);
}