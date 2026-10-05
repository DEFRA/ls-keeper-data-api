using FluentAssertions;
using KeeperData.Core.Exceptions;
using Xunit;

namespace KeeperData.Core.Tests.Unit.Exceptions;

public class SearchIndexUnavailableExceptionTests
{
    [Fact]
    public void DefaultConstructor_SetsDefaultMessage()
    {
        var ex = new SearchIndexUnavailableException();
        ex.Message.Should().Be("The holding search index is not available.");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void MessageConstructor_SetsCustomMessage()
    {
        var ex = new SearchIndexUnavailableException("Custom error message");
        ex.Message.Should().Be("Custom error message");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_SetsMessageAndInnerException()
    {
        var inner = new InvalidOperationException("inner error");
        var ex = new SearchIndexUnavailableException("Custom error message", inner);
        ex.Message.Should().Be("Custom error message");
        ex.InnerException.Should().BeSameAs(inner);
    }
}