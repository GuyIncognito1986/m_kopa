using MKopa.SMS.Worker.Lib.DomainModel;
using MKopa.SMS.Worker.Lib.Exceptions;
using NUnit.Framework;
using Visus.Cuid;

namespace MKopa.SMS.Worker.UnitTests
{
    [TestFixture]
    public class SendSmsCommandTests
    {
        [Test]
        public void IfAllValidationFailsShouldThrowAndContainTheRightMessage()
        {
            var phoneNumberBuilder = new PhoneNumbers.PhoneNumber.Builder();
            phoneNumberBuilder.RawInput = "+000000000000";
            var str = new String('c', 161);
            var command = new SendSmsCommand(new Cuid2(), phoneNumberBuilder.Build(), str);
            var ex = Assert.Throws<SendSmsCommandValidationException>(() => command.Validate());
            Assert.That(ex.Message, Is.EqualTo(SendSmsCommand.SMSTextTooLongError));
        }

        [Test]
        public void IfAllValidationDoesNotFailShouldNotThrow()
        {
            var phoneNumberBuilder = new PhoneNumbers.PhoneNumber.Builder();
            phoneNumberBuilder.RawInput = "+447700900000";
            var str = new String('c', 160);
            var command = new SendSmsCommand(new Cuid2(), phoneNumberBuilder.Build(), str);
            Assert.DoesNotThrow(() => command.Validate());
        }
    }
}
