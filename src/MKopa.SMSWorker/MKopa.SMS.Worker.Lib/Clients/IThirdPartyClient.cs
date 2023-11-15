namespace MKopa.SMS.Worker.Lib.Clients;

using DomainModel;

public interface IThirdPartyClient
{
    public Task<ResponseType> PostMessageSentEventToThirdPartyApiAsync(ThirdPartySmsMessage message);
}