using System.Collections.Concurrent;
using MKopa.SMS.Worker.Lib.StateMachines;
using Visus.Cuid;

namespace MKopa.SMS.Worker.Lib.Clients;

public interface ISmsStateServiceClient
{
    public Task<Cuid2> GetState(SMSStateMachine.States state);
    public Task<ConcurrentDictionary<Cuid2, SMSStateMachine.States>> GetAllStates();
}