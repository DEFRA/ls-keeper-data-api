using FluentAssertions;
using KeeperData.Core.Messaging.Contracts;
using KeeperData.Core.Messaging.MessageHandlers;
using KeeperData.Infrastructure.Messaging.MessageHandlers;

namespace KeeperData.Infrastructure.Tests.Unit.Messaging.MessageHandlers;

public class InMemoryMessageHandlerManagerTests
{
    private readonly InMemoryMessageHandlerManager _sut = new();

    [Fact]
    public void AddReceiver_ShouldRegisterHandlerAndMessageType()
    {
        _sut.AddReceiver<InMemoryTestMessage, InMemoryTestHandler>();

        _sut.HasHandlerForMessage<InMemoryTestMessage>().Should().BeTrue();
        _sut.GetMessageTypeByName("InMemoryTestMessage").Should().Be(typeof(InMemoryTestMessage));
        _sut.GetHandlersForMessage<InMemoryTestMessage>().Should().ContainSingle()
            .Which.HandlerType.Should().Be(typeof(InMemoryTestHandler));
    }

    [Fact]
    public void AddReceiver_ShouldStripSuffixFromMessageTypeKey()
    {
        var key = _sut.GetMessageTypeKey<InMemoryTestMessage>();
        key.Should().Be("InMemoryTest");
    }

    [Fact]
    public void AddReceiver_ShouldThrow_WhenHandlerAlreadyRegistered()
    {
        _sut.AddReceiver<InMemoryTestMessage, InMemoryTestHandler>();

        Action act = () => _sut.AddReceiver<InMemoryTestMessage, InMemoryTestHandler>();
        act.Should().Throw<ArgumentException>()
            .WithMessage("Handler Type InMemoryTestHandler already registered for 'InMemoryTest'*");
    }

    [Fact]
    public void HasHandlerForMessage_ShouldReturnFalse_WhenNoHandlerExists()
    {
        _sut.HasHandlerForMessage("Unknown").Should().BeFalse();
    }

    [Fact]
    public void GetHandlersForMessage_ShouldReturnCorrectHandlerList()
    {
        _sut.AddReceiver<InMemoryTestMessage, InMemoryTestHandler>();

        var handlers = _sut.GetHandlersForMessage("InMemoryTestMessage");
        handlers.Should().ContainSingle()
            .Which.HandlerType.Should().Be(typeof(InMemoryTestHandler));
    }

    [Fact]
    public void GetMessageTypeByName_ShouldReturnNull_WhenTypeNotRegistered()
    {
        var result = _sut.GetMessageTypeByName("UnregisteredType");
        result.Should().BeNull();
    }

    public class InMemoryTestMessage : MessageType { }
    public class InMemoryTestHandler : IMessageHandler<InMemoryTestMessage>
    {
        public async Task<MessageType> Handle(UnwrappedMessage message, CancellationToken cancellationToken)
        {
            return await Task.FromResult(new InMemoryTestMessage());
        }
    }
}
