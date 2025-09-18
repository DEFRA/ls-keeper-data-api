using Microsoft.Extensions.Logging;
using Moq;

namespace KeeperData.Infrastructure.Tests.Unit.Helpers;

public static class LoggerMockExtensions
{
    public static void VerifyLog<T>(
        this Mock<ILogger<T>> loggerMock,
        LogLevel level,
        string expectedMessage,
        Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}