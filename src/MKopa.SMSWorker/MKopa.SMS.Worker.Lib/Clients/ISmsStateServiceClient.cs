using System.Collections.Concurrent;
using MKopa.SMS.Worker.Lib.StateMachines;
using Visus.Cuid;

namespace MKopa.SMS.Worker.Lib.Clients;

public interface ISmsStateServiceClient
{
    public Task<SMSStateMachine.States> GetStateAsync(Cuid2 correlationId);
}