namespace MKopa.SMS.Worker.Lib.StateMachines;

using Microsoft.Extensions.Logging;
using Clients;
using Exceptions;
using DomainModel;
using Visus.Cuid;
using System.Reflection.Metadata.Ecma335;

public class SMSStateMachine: IDisposable
{
    public enum States
    {
        Initial = 0,
        Running = 1,
        MessageSendSuccessful = 2,
        MessageDeadLettered = 3,
        MessageDeserialized = 4,
        MessageValidated = 5
    }

    private States _currentState = States.Initial;
    private readonly IQueueClient _queueClient;
    private readonly IServiceBusClient _serviceBusClient;
    private readonly ISmsStateServiceClient _stateServiceClient;
    private readonly IThirdPartyClient _thirdPartyClient;
    private readonly ILogger _logger;
    private readonly byte[] _message;
    private SendSmsCommand? _deserializedCommandMessage { get; set; }
    private SemaphoreSlim _stateSemaphore = new SemaphoreSlim(1);
    private SemaphoreSlim _deserializedMessageSemaphore = new SemaphoreSlim(1);

    public SMSStateMachine(IQueueClient queueClient, 
        IServiceBusClient serviceBusClient, 
        ISmsStateServiceClient stateServiceClient, 
        IThirdPartyClient thirdPartyClient,
        ILogger logger,
        byte[] message)
    {
        _queueClient = queueClient;
        _serviceBusClient = serviceBusClient;
        _thirdPartyClient = thirdPartyClient;
        _stateServiceClient = stateServiceClient;
        _logger = logger;
        _message = message;
    }

    public async Task<bool> IsRunning()
    {
        var state = await SafelyGetState();
        if (state == States.MessageDeadLettered || state == States.Initial || state == States.MessageSendSuccessful) return false;
        return true;
    }

    public async Task<bool> HasFinished()
    {
        var state = await SafelyGetState();
        if (state == States.MessageDeadLettered || state == States.MessageSendSuccessful) return true;
        return false;
    }

    public async Task RunStateMachine()
    {
        if(await IsRunning())
        {
            _logger.LogWarning("State machine already running");
            return;
        }
        await SafelyUpdateState(States.Running);
        _logger.LogInformation($"Starting sms state machine, current state: {await SafelyGetState()}");
        while (await IsRunning())
        {
            await ProcessState();
        }
    }

    private async Task SafelyUpdateState(States state)
    {
        try
        {
            await _stateSemaphore.WaitAsync();
            _currentState = state;
        }
        finally
        {
            _stateSemaphore.Release();
        }
    }

    private async Task<States> SafelyGetState()
    {
        try
        {
            await _stateSemaphore.WaitAsync();
            return _currentState;
        }
        finally
        {
            _stateSemaphore.Release();
        }
    }

    private async Task SafelyUpdateDesrializedMessage(SendSmsCommand sendSmsCommand)
    {
        try
        {
            await _deserializedMessageSemaphore.WaitAsync();
            _deserializedCommandMessage = sendSmsCommand;
        }
        finally
        {
            _deserializedMessageSemaphore.Release();
        }
    }

    private async Task<SendSmsCommand> SafelyGetDeserializedMessage()
    {
        try
        {
            await _deserializedMessageSemaphore.WaitAsync();
            if (_deserializedCommandMessage != null)
                return _deserializedCommandMessage;
            else
                throw new DeserializedMessageCanNotBeNullException();
        }
        finally
        {
            _deserializedMessageSemaphore.Release();
        }
    }

    private async Task ToDeadLetterAsync(byte[] message, Cuid2? cuid)
    {
        await _serviceBusClient.PublishToDeadLetterAsync(message);
        await SafelyUpdateState(States.MessageDeadLettered);
        var id = cuid == null ? new Cuid2() : cuid.Value;
        _logger.LogError($"Sending to dead letter with message id {id}");
        if (cuid != null)
        {
            await _stateServiceClient.SetStateAsync(cuid.Value, States.MessageDeadLettered);
        }
        else
        {
            await _stateServiceClient.SetStateAsync(new Cuid2(), States.MessageDeadLettered);
        }
        
    }

    private async Task ProcessState()
    {
        switch (_currentState)
        {
            case States.Running:
                try
                {
                    var dMsg = _queueClient.Deserialize(_message);
                    await SafelyUpdateDesrializedMessage(dMsg);
                    await SafelyUpdateState(States.MessageDeserialized);
                    var stateExistsInStateService = await _stateServiceClient.GetStateAsync(dMsg.CorrelationId);
                    if (stateExistsInStateService != null && 
                        (stateExistsInStateService == States.MessageDeadLettered ||
                        stateExistsInStateService == States.MessageSendSuccessful))
                    {
                        _logger.LogInformation($"Message with correlation id {dMsg.CorrelationId} has already been processed");
                        await SafelyUpdateState(stateExistsInStateService.Value);
                        return;
                    }
                    _logger.LogInformation($"Deserialized message with correlation id: {dMsg.CorrelationId}");
                    _logger.LogTrace($"With number: {dMsg.PhoneNumber}\nAnd body: {dMsg.Text}");
                }
                catch (QueueDeserializationFailedException)
                {
                    _logger.LogError("Failed to deserialize sms command from the queue! Pushing to dead letter!");
                    var failedDeserializationMessage = new FailedDeserializationDeadLetterMessage(_message);
                    var deadLetterMessage = _serviceBusClient.Serialize(failedDeserializationMessage);
                    await ToDeadLetterAsync(deadLetterMessage, null);
                }
                break;
            case States.MessageDeserialized:
                var message = await SafelyGetDeserializedMessage();
                try
                {
                    message.Validate();
                    await SafelyUpdateState(States.MessageValidated);
                    _logger.LogInformation($"Validated message with correlation id: {message.CorrelationId}");
                }
                catch (SendSmsCommandValidationException e)
                {
                    var validationFailedMessage = new FailedCommandDeadLetterMessage(message, e.Message);
                    var deadLetterMessage = _serviceBusClient.Serialize(validationFailedMessage);
                    await ToDeadLetterAsync(deadLetterMessage, message.CorrelationId);
                }
                break;
            case States.MessageValidated:
                var msg = await SafelyGetDeserializedMessage();
                var resp = await _thirdPartyClient.PostMessageSentEventToThirdPartyApiAsync(msg.ToThirdPartySmsMessage());
                if (resp == ResponseType.Success)
                {
                    _currentState = States.MessageSendSuccessful;
                    var messageSent = new MessageSent(msg.CorrelationId);
                    var sMessageSent = _serviceBusClient.Serialize(messageSent);
                    await _serviceBusClient.PublishToMessageSendCompletedAsync(sMessageSent);
                    await _stateServiceClient.SetStateAsync(msg.CorrelationId, States.MessageSendSuccessful);
                    _logger.LogInformation($"Sent message with correlation id: {msg.CorrelationId}");
                }
                else
                {
                    var validationFailedMessage = new FailedCommandDeadLetterMessage(msg, "Fatal error when using 3rd party api!");
                    var deadLetterMessage = _serviceBusClient.Serialize(validationFailedMessage);
                    await ToDeadLetterAsync(deadLetterMessage, msg.CorrelationId);
                }
                break;
            default:
                var state = await SafelyGetState();
                _logger.LogWarning($"State machine is in finite or has not started yet, current state: {state}");
                break;
        }
    }
    
    public void Dispose()
    {
        _stateSemaphore.Dispose();
        _deserializedMessageSemaphore.Dispose();
    }
}