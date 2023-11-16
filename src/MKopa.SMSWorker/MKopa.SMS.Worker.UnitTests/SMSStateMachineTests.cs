namespace MKopa.SMS.Worker.UnitTests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using FakeItEasy;
using MKopa.SMS.Worker.Lib.Clients;
using MKopa.SMS.Worker.Lib.DomainModel;
using PhoneNumbers;
using Visus.Cuid;
using MKopa.SMS.Worker.Lib.StateMachines;
using MKopa.SMS.Worker.Lib.Exceptions;
using System;

[TestFixture]
public class SMSStateMachineTests
{
    private ILogger _logger = NullLogger.Instance;
    private string _textMessage = "Test text";
    
    private PhoneNumber GetPhoneNumber()
    {
        var phoneNumberBuilder = new PhoneNumbers.PhoneNumber.Builder();
        phoneNumberBuilder.RawInput = "+447700900000";
        return phoneNumberBuilder.Build();
    }

    [Test]
    public async Task HappyPath()
    {
        var fakedQueueClient = A.Fake<IQueueClient>();
        A.CallTo(() => fakedQueueClient.Deserialize(Array.Empty<byte>())).Returns(new SendSmsCommand(new Cuid2(), GetPhoneNumber(), _textMessage));
        var fakedServiceBusClient = A.Fake<IServiceBusClient>();
        A.CallTo(() => fakedServiceBusClient.PublishToMessageSendCompletedAsync(A<byte[]>._)).Returns(Task.FromResult(0));
        var fakedSmsStateServiceClient = A.Fake<ISmsStateServiceClient>();
        A.CallTo(() => fakedSmsStateServiceClient.SetStateAsync(new Cuid2(), Lib.StateMachines.SMSStateMachine.States.MessageSendSuccessful)).Returns(Task.FromResult(0));
        var fakedThirdPartyClient = A.Fake<IThirdPartyClient>();
        A.CallTo(() => fakedThirdPartyClient.PostMessageSentEventToThirdPartyApiAsync(A<ThirdPartySmsMessage>._)).Returns(Task.FromResult(ResponseType.Success));
        var stateMachine = new SMSStateMachine(fakedQueueClient, fakedServiceBusClient, fakedSmsStateServiceClient, fakedThirdPartyClient, _logger, Array.Empty<byte>());
        await stateMachine.RunStateMachine();
        A.CallTo(() => fakedThirdPartyClient.PostMessageSentEventToThirdPartyApiAsync(A<ThirdPartySmsMessage>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fakedServiceBusClient.PublishToMessageSendCompletedAsync(A<byte[]>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fakedThirdPartyClient.PostMessageSentEventToThirdPartyApiAsync(A<ThirdPartySmsMessage>._)).MustHaveHappenedOnceExactly();
    }
    
    [Test]
    public async Task HappyPathMessageAlreadyProcessed()
    {
        var fakedQueueClient = A.Fake<IQueueClient>();
        A.CallTo(() => fakedQueueClient.Deserialize(Array.Empty<byte>())).Returns(new SendSmsCommand(new Cuid2(), GetPhoneNumber(), _textMessage));
        var fakedServiceBusClient = A.Fake<IServiceBusClient>();
        A.CallTo(() => fakedServiceBusClient.PublishToMessageSendCompletedAsync(A<byte[]>._)).Returns(Task.FromResult(0));
        var fakedSmsStateServiceClient = A.Fake<ISmsStateServiceClient>();
        A.CallTo(() => fakedSmsStateServiceClient.SetStateAsync(new Cuid2(), Lib.StateMachines.SMSStateMachine.States.MessageSendSuccessful)).Returns(Task.FromResult(0));
        A.CallTo(() => fakedSmsStateServiceClient.GetStateAsync(A<Cuid2>._)).Returns(Task.FromResult((SMSStateMachine.States?)SMSStateMachine.States.MessageSendSuccessful));
        var fakedThirdPartyClient = A.Fake<IThirdPartyClient>();
        var stateMachine = new SMSStateMachine(fakedQueueClient, fakedServiceBusClient, fakedSmsStateServiceClient, fakedThirdPartyClient, _logger, Array.Empty<byte>());
        await stateMachine.RunStateMachine();
        Assert.IsTrue(await stateMachine.HasFinished());
    }

    [Test]
    public async Task FailedDeserialization()
    {
        var fakedQueueClient = A.Fake<IQueueClient>();
        A.CallTo(() => fakedQueueClient.Deserialize(Array.Empty<byte>())).Throws<QueueDeserializationFailedException>();
        var fakedServiceBusClient = A.Fake<IServiceBusClient>();
        A.CallTo(() => fakedServiceBusClient.PublishToDeadLetterAsync(A<byte[]>._)).Returns(Task.FromResult(0));
        var fakedSmsStateServiceClient = A.Fake<ISmsStateServiceClient>();
        A.CallTo(() => fakedSmsStateServiceClient.SetStateAsync(A<Cuid2>._, Lib.StateMachines.SMSStateMachine.States.MessageDeadLettered)).Returns(Task.FromResult(0));
        var fakedThirdPartyClient = A.Fake<IThirdPartyClient>();
        var stateMachine = new SMSStateMachine(fakedQueueClient, fakedServiceBusClient, fakedSmsStateServiceClient, fakedThirdPartyClient, _logger, Array.Empty<byte>());
        await stateMachine.RunStateMachine();
        A.CallTo(() => fakedServiceBusClient.PublishToDeadLetterAsync(A<byte[]>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fakedSmsStateServiceClient.SetStateAsync(A<Cuid2>._, Lib.StateMachines.SMSStateMachine.States.MessageDeadLettered)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task FailedValidation()
    {
        var fakedQueueClient = A.Fake<IQueueClient>();
        A.CallTo(() => fakedQueueClient.Deserialize(Array.Empty<byte>())).Returns(new SendSmsCommand(new Cuid2(), GetPhoneNumber(), new String('s', 161)));
        var fakedServiceBusClient = A.Fake<IServiceBusClient>();
        A.CallTo(() => fakedServiceBusClient.PublishToDeadLetterAsync(A<byte[]>._)).Returns(Task.FromResult(0));
        var fakedSmsStateServiceClient = A.Fake<ISmsStateServiceClient>();
        A.CallTo(() => fakedSmsStateServiceClient.SetStateAsync(A<Cuid2>._, Lib.StateMachines.SMSStateMachine.States.MessageDeadLettered)).Returns(Task.FromResult(0));
        var fakedThirdPartyClient = A.Fake<IThirdPartyClient>();
        var stateMachine = new SMSStateMachine(fakedQueueClient, fakedServiceBusClient, fakedSmsStateServiceClient, fakedThirdPartyClient, _logger, Array.Empty<byte>());
        await stateMachine.RunStateMachine();
        A.CallTo(() => fakedServiceBusClient.PublishToDeadLetterAsync(A<byte[]>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fakedSmsStateServiceClient.SetStateAsync(A<Cuid2>._, Lib.StateMachines.SMSStateMachine.States.MessageDeadLettered)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task FailedThirdPartySend()
    {
        var fakedQueueClient = A.Fake<IQueueClient>();
        A.CallTo(() => fakedQueueClient.Deserialize(Array.Empty<byte>())).Returns(new SendSmsCommand(new Cuid2(), GetPhoneNumber(), new String('s', 161)));
        var fakedServiceBusClient = A.Fake<IServiceBusClient>();
        A.CallTo(() => fakedServiceBusClient.PublishToDeadLetterAsync(A<byte[]>._)).Returns(Task.FromResult(0));
        var fakedSmsStateServiceClient = A.Fake<ISmsStateServiceClient>();
        A.CallTo(() => fakedSmsStateServiceClient.SetStateAsync(A<Cuid2>._, Lib.StateMachines.SMSStateMachine.States.MessageDeadLettered)).Returns(Task.FromResult(0));
        var fakedThirdPartyClient = A.Fake<IThirdPartyClient>();
        A.CallTo(() => fakedThirdPartyClient.PostMessageSentEventToThirdPartyApiAsync(A<ThirdPartySmsMessage>._)).Returns(Task.FromResult(ResponseType.Fatal));
        var stateMachine = new SMSStateMachine(fakedQueueClient, fakedServiceBusClient, fakedSmsStateServiceClient, fakedThirdPartyClient, _logger, Array.Empty<byte>());
        await stateMachine.RunStateMachine();
        A.CallTo(() => fakedServiceBusClient.PublishToDeadLetterAsync(A<byte[]>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fakedSmsStateServiceClient.SetStateAsync(A<Cuid2>._, Lib.StateMachines.SMSStateMachine.States.MessageDeadLettered)).MustHaveHappenedOnceExactly();
    }
}

