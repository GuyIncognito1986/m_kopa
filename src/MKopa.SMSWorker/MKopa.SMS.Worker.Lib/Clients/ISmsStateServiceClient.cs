namespace MKopa.SMS.Worker.Lib.Clients;

using MKopa.SMS.Worker.Lib.StateMachines;
using Visus.Cuid;

public interface ISmsStateServiceClient
{
    public Task<SMSStateMachine.States?> GetStateAsync(Cuid2 correlationId);
    public Task SetStateAsync(Cuid2 correlationId, SMSStateMachine.States state);
}