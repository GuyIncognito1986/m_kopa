namespace MKopa.SMS.Worker.Lib.DomainModel;

public class FailedCommandValidationDeadLetterMessage
{
    public SendSmsCommand FailedCommand { get; }
    public string ErrorMessage { get; }

    public FailedCommandValidationDeadLetterMessage(SendSmsCommand failedCommand, string errorMessage)
    {
        FailedCommand = failedCommand;
        ErrorMessage = errorMessage;
    }
}
