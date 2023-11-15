namespace MKopa.SMS.Worker.Lib.StateMachines;

using Microsoft.Extensions.Logging;
using Clients;
using Exceptions;
using DomainModel;
using Visus.Cuid;

public class SMSStateMachine
{
    public enum States
    {
        Initial = 0,
        MessageSendSuccessful = 1,
        MessageDeadLettered = 2,
        MessageDeserialized = 3,
        MessageValidated = 4
    }

    private States _currentState = States.Initial;
    private readonly IQueueClient _queueClient;
    private readonly IServiceBusClient _serviceBusClient;
    private readonly ISmsStateServiceClient _stateServiceClient;
    private readonly IThirdPartyClient _thirdPartyClient;
    private readonly ILogger<SMSStateMachine> _logger;
    private readonly byte[] _message;
    
    public SMSStateMachine(IQueueClient queueClient, 
        IServiceBusClient serviceBusClient, 
        ISmsStateServiceClient stateServiceClient, 
        IThirdPartyClient thirdPartyClient,
        ILogger<SMSStateMachine> logger,
        byte[] message)
    {
        _queueClient = queueClient;
        _serviceBusClient = serviceBusClient;
        _thirdPartyClient = thirdPartyClient;
        _stateServiceClient = stateServiceClient;
        _logger = logger;
        _message = message;
    }
    
    public async Task RunStateMachine()
    {
        _logger.LogInformation($"Starting sms state machine processing in state: {_currentState.ToString()}");
        SendSmsCommand? sendSmsCommand = null;
        while (_currentState != States.MessageDeadLettered && _currentState != States.MessageSendSuccessful)
        {
            sendSmsCommand = await ProcessState(sendSmsCommand);
        }
    }

    private async Task<SendSmsCommand?> ProcessState(SendSmsCommand? msg)
    {
        switch (_currentState)
        {
            case States.Initial:
                try
                {
                    var message = _queueClient.Deserialize(_message);
                    _logger.LogInformation($"Deserialized message with correlation id: {message.CorrelationId}");
                    _logger.LogTrace($"With number: {message.PhoneNumber}\nAnd body: {message.Text}");
                    _currentState = States.MessageDeserialized;
                    return message;
                }
                catch (QueueDeserializationFailedException)
                {
                    _logger.LogError("Failed to deserialize sms command from the queue! Pushing to dead letter!");
                    var failedDeserializationMessage = new FailedDeserializationDeadLetterMessage(_message);
                    var deadLetterMessage = _serviceBusClient.Serialize(failedDeserializationMessage); 
                    await _serviceBusClient.PublishToDeadLetterAsync(deadLetterMessage);
                    _currentState = States.MessageDeadLettered;
                    await _stateServiceClient.SetStateAsync(new Cuid2(), States.MessageDeadLettered);
                    return null;
                }
            case States.MessageDeserialized:
                try
                {
                    msg?.Validate();
                    _currentState = States.MessageValidated;
                }
                catch (SendSmsCommandValidationException e)
                {
                    var validationFailedMessage = new FailedCommandDeadLetterMessage(msg, e.Message);
                    var deadLetterMessage = _serviceBusClient.Serialize(validationFailedMessage);
                    await _serviceBusClient.PublishToDeadLetterAsync(deadLetterMessage);
                    _currentState = States.MessageDeadLettered;
                    await _stateServiceClient.SetStateAsync(msg.CorrelationId, States.MessageDeadLettered);
                }
                return msg;
            case States.MessageValidated:
                var resp = await _thirdPartyClient.PostMessageSentEventToThirdPartyApiAsync(msg.ToThirdPartySmsMessage());
                if (resp == ResponseType.CanRetry)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        resp = await _thirdPartyClient.PostMessageSentEventToThirdPartyApiAsync(msg.ToThirdPartySmsMessage());
                        if (resp == ResponseType.Success || resp == ResponseType.Fatal) break;
                    }    
                }
                if (resp == ResponseType.Success)
                {
                    _currentState = States.MessageSendSuccessful;
                    var messageSent = new MessageSent(msg.CorrelationId);
                    var sMessageSent = _serviceBusClient.Serialize(messageSent);
                    await _serviceBusClient.PublishToMessageSendCompletedAsync(sMessageSent);
                    await _stateServiceClient.SetStateAsync(msg.CorrelationId, States.MessageSendSuccessful);
                }
                else
                {
                    var validationFailedMessage = new FailedCommandDeadLetterMessage(msg, "Fatal error when using 3rd party api!");
                    var deadLetterMessage = _serviceBusClient.Serialize(validationFailedMessage);
                    await _serviceBusClient.PublishToDeadLetterAsync(deadLetterMessage);
                    _currentState = States.MessageDeadLettered;
                    await _stateServiceClient.SetStateAsync(msg.CorrelationId, States.MessageDeadLettered);
                }
                return msg;
            default:
                throw new Exception("Unknown state");
        }
    }
}