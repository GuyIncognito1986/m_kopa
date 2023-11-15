namespace MKopa.SMS.Worker.Lib.DomainModel;

using StateMachines;
using Visus.Cuid;

public class SMSMessageState
{
    public Cuid2 CorrelationId { get; }
    public SMSStateMachine.States State { get; }

    public SMSMessageState(Cuid2 correlationId, SMSStateMachine.States state)
    {
        CorrelationId = correlationId;
        State = state;
    }
}