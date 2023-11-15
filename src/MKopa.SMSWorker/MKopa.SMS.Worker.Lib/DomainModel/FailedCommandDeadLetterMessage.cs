namespace MKopa.SMS.Worker.Lib.DomainModel;

public class FailedCommandDeadLetterMessage
{
    public SendSmsCommand? FailedCommand { get; }
    public string ErrorMessage { get; }

    public FailedCommandDeadLetterMessage(SendSmsCommand? failedCommand, string errorMessage)
    {
        FailedCommand = failedCommand;
        ErrorMessage = errorMessage;
    }
}
