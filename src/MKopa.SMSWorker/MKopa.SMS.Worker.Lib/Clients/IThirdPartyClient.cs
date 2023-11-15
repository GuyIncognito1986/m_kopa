namespace MKopa.SMS.Worker.Lib.Clients;

public interface IThirdPartyClient
{
    public Task<HttpResponseMessage> PostMessageSentEventToThirdPartyApiAsync();
}