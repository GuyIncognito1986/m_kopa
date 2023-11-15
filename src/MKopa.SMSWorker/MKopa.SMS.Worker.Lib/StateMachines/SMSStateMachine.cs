namespace MKopa.SMS.Worker.Lib.StateMachines;

public class SMSStateMachine
{
    public enum States
    {
        MessageReceivedFromQueue = 0,
        MessageProcessed = 1,
        MessageSent = 2,
        MessageDeadLettered = 3
    }

    public SMSStateMachine()
    {
        
    }
    
    public void ToDeadLetter()
    {
        
    }
    
    
}