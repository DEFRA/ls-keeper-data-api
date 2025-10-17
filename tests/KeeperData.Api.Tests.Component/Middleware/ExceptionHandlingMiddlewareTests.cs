using FluentAssertions;
using FluentValidation.Results;
using KeeperData.Api.Middleware;
using KeeperData.Core.Exceptions;
using KeeperData.Infrastructure;
using KeeperData.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Text.Json;

namespace KeeperData.Api.Tests.Component.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly TestLogger<ExceptionHandlingMiddleware> _testLogger;
    private readonly Mock<IApplicationMetrics> _mockMetrics;
    private readonly string _traceHeader = "x-cdp-request-id";

    public ExceptionHandlingMiddlewareTests()
    {
        _testLogger = new TestLogger<ExceptionHandlingMiddleware>();
        _mockMetrics = new Mock<IApplicationMetrics>();
    }

    public class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new TestScope();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                State = state,
                Exception = exception,
                Message = formatter(state, exception)
            });
        }

        private class TestScope : IDisposable
        {
            public void Dispose() { }
        }
    }

    public class LogEntry
    {
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public object? State { get; set; }
        public Exception? Exception { get; set; }
        public string? Message { get; set; }
    }

    public class TestConfiguration(Dictionary<string, string> data) : IConfiguration
    {
        private readonly Dictionary<string, string> _data = data;

        public string? this[string key]
        {
            get => _data.TryGetValue(key, out var value) ? value : null;
            set => _data[key] = value!;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];
        public IChangeToken GetReloadToken() => default!;
        public IConfigurationSection GetSection(string key) => default!;
    }

    private ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        var initialData = new Dictionary<string, string>
        {
            ["TraceHeader"] = _traceHeader
        } as IEnumerable<KeyValuePair<string, string?>>;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(initialData)
            .Build();

        return new ExceptionHandlingMiddleware(next, _testLogger, config, _mockMetrics.Object);
    }

    private DefaultHttpContext CreateHttpContext(string path = "/test", string? traceHeaderValue = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = "GET";
        context.Response.Body = new MemoryStream();

        if (traceHeaderValue != null)
        {
            context.Request.Headers[_traceHeader] = traceHeaderValue;
        }

        return context;
    }

    private static async Task<ProblemDetails> GetProblemDetailsFromResponse(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<ProblemDetails>(json, JsonDefaults.DefaultOptionsWithStringEnumConversion)!;
    }

    [Fact]
    public async Task NotFoundException_returns_404()
    {
        var context = CreateHttpContext("/test-path");
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Sheep", 42));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(404);
        context.Response.ContentType.Should().Be("application/json");

        var problem = await GetProblemDetailsFromResponse(context);
        problem.Status.Should().Be(404);
        problem.Title.Should().Be("Not Found");
        problem.Detail.Should().Contain("'Sheep' (42) was not found.");
        problem.Instance.Should().Be("/test-path");
        problem.Extensions.Should().ContainKey("traceId");
        problem.Extensions.Should().ContainKey("correlationId");
        problem.Extensions.Should().ContainKey("errorId");
    }

    [Fact]
    public async Task Other_exception_returns_500()
    {
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("internal error"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);

        var problem = await GetProblemDetailsFromResponse(context);
        problem.Status.Should().Be(500);
        problem.Title.Should().Be("An error occurred");
        problem.Detail.Should().Contain("internal error");
        problem.Extensions.Should().ContainKey("traceId");
        problem.Extensions.Should().ContainKey("correlationId");
        problem.Extensions.Should().ContainKey("errorId");
    }

    [Fact]
    public async Task FluentValidationException_returns_422_with_structured_errors()
    {
        var validationFailures = new List<ValidationFailure>
        {
            new("Email", "'Email' is not a valid email address."),
            new("CPH", "'CPH' is not valid."),
        };
        var exceptionToThrow = new FluentValidation.ValidationException(validationFailures);

        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw exceptionToThrow);

        await middleware.InvokeAsync(context);
        context.Response.StatusCode.Should().Be(422);

        var problem = await GetProblemDetailsFromResponse(context);
        problem.Status.Should().Be(422);

        problem.Title.Should().Be("Unprocessable Content");
        problem.Detail.Should().Be("One or more validation errors occurred. See the 'errors' field for details.");

        problem.Extensions.Should().ContainKey("errors");

        var errorsElement = (JsonElement)problem.Extensions["errors"]!;
        var errors = errorsElement.Deserialize<Dictionary<string, string[]>>();

        errors.Should().NotBeNull();
        errors.Should().HaveCount(2);

        errors.Should().ContainKey("Email");
        errors!["Email"].Should().Contain("'Email' is not a valid email address.");

        errors.Should().ContainKey("CPH");
        errors["CPH"].Should().Contain("'CPH' is not valid.");
    }

    [Fact]
    public async Task DomainException_returns_400()
    {
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new DomainException("Domain error"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);

        var problem = await GetProblemDetailsFromResponse(context);
        problem.Status.Should().Be(400);
        problem.Title.Should().Be("Bad Request");
    }

    [Fact]
    public async Task Response_has_correct_content_type()
    {
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Test", 1));

        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task Response_contains_errorId_extension()
    {
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Test", 1));

        await middleware.InvokeAsync(context);

        var problem = await GetProblemDetailsFromResponse(context);
        problem.Extensions.Should().ContainKey("errorId");
        problem.Extensions["errorId"].Should().NotBeNull();

        Guid.TryParse(problem.Extensions["errorId"]?.ToString(), out _).Should().BeTrue();
    }

    [Fact]
    public async Task TraceId_and_correlationId_are_same_when_no_header()
    {
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Test", 1));

        await middleware.InvokeAsync(context);

        var problem = await GetProblemDetailsFromResponse(context);
        var traceId = problem.Extensions["traceId"]?.ToString();
        var correlationId = problem.Extensions["correlationId"]?.ToString();

        traceId.Should().Be(correlationId);
        traceId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Uses_custom_trace_header_when_provided()
    {
        var customTraceId = "custom-trace-12345";
        var context = CreateHttpContext(traceHeaderValue: customTraceId);
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Test", 1));

        await middleware.InvokeAsync(context);

        var problem = await GetProblemDetailsFromResponse(context);
        problem.Extensions["traceId"]?.ToString().Should().Be(customTraceId);
        problem.Extensions["correlationId"]?.ToString().Should().Be(customTraceId);
    }

    [Fact]
    public async Task Uses_first_value_when_multiple_trace_headers()
    {
        var firstTraceId = "first-trace-12345";
        var secondTraceId = "second-trace-67890";
        var context = CreateHttpContext();
        context.Request.Headers[_traceHeader] = new[] { firstTraceId, secondTraceId };
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Test", 1));

        await middleware.InvokeAsync(context);

        var problem = await GetProblemDetailsFromResponse(context);
        problem.Extensions["traceId"]?.ToString().Should().Be(firstTraceId);
    }

    [Fact]
    public async Task Each_request_gets_unique_errorId()
    {
        var context1 = CreateHttpContext();
        var context2 = CreateHttpContext();
        var middleware1 = CreateMiddleware(_ => throw new NotFoundException("Test", 1));
        var middleware2 = CreateMiddleware(_ => throw new NotFoundException("Test", 2));

        await middleware1.InvokeAsync(context1);
        await middleware2.InvokeAsync(context2);

        var problem1 = await GetProblemDetailsFromResponse(context1);
        var problem2 = await GetProblemDetailsFromResponse(context2);

        var errorId1 = problem1.Extensions["errorId"]?.ToString();
        var errorId2 = problem2.Extensions["errorId"]?.ToString();

        errorId1.Should().NotBe(errorId2);
        errorId1.Should().NotBeNullOrEmpty();
        errorId2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Exception_with_empty_message_handled_gracefully()
    {
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException(string.Empty));

        await middleware.InvokeAsync(context);

        var problem = await GetProblemDetailsFromResponse(context);
        problem.Detail.Should().BeEmpty();
    }

    [Fact]
    public async Task Response_uses_camelCase_property_naming()
    {
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Test", 1));

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var jsonString = await reader.ReadToEndAsync();

        jsonString.Should().Contain("\"title\":");
        jsonString.Should().Contain("\"detail\":");
        jsonString.Should().Contain("\"status\":");
        jsonString.Should().Contain("\"instance\":");
        jsonString.Should().Contain("\"traceId\":");
        jsonString.Should().Contain("\"correlationId\":");
        jsonString.Should().Contain("\"errorId\":");
    }

    [Fact]
    public async Task Response_json_is_properly_indented()
    {
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Test", 1));

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var jsonString = await reader.ReadToEndAsync();

        jsonString.Should().Contain("\n");
        jsonString.Should().Contain("  "); // Indentation spaces
    }

    [Fact]
    public async Task InternalServerError_logs_as_error_level()
    {
        _testLogger.LogEntries.Clear();
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("internal error"));

        await middleware.InvokeAsync(context);

        _testLogger.LogEntries.Should().HaveCount(1);
        var logEntry = _testLogger.LogEntries.First();

        logEntry.LogLevel.Should().Be(LogLevel.Error);
        logEntry.Exception.Should().NotBeNull();
        logEntry.Exception.Should().BeOfType<InvalidOperationException>();
        logEntry.Message.Should().Contain("ErrorId:");
        logEntry.Message.Should().Contain("CorrelationId:");
        logEntry.Message.Should().Contain("StatusCode: 500");
    }

    [Fact]
    public async Task ClientError_logs_as_information_level()
    {
        _testLogger.LogEntries.Clear();
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Test", 1));

        await middleware.InvokeAsync(context);

        _testLogger.LogEntries.Should().HaveCount(1);
        var logEntry = _testLogger.LogEntries.First();

        logEntry.LogLevel.Should().Be(LogLevel.Information);
        logEntry.Exception.Should().NotBeNull();
        logEntry.Exception.Should().BeOfType<NotFoundException>();
        logEntry.Message.Should().Contain("ErrorId:");
        logEntry.Message.Should().Contain("CorrelationId:");
        logEntry.Message.Should().Contain("StatusCode: 404");
    }

    [Fact]
    public async Task Successful_request_passes_through_without_modification()
    {
        var context = CreateHttpContext();
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Successful_request_records_success_metrics()
    {
        // Arrange
        var context = CreateHttpContext("/api/test");
        var nextCalled = false;
        var sut = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await sut.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);

        _mockMetrics.Verify(m => m.RecordRequest("http_request", "success"), Times.Once);
        _mockMetrics.Verify(m => m.RecordDuration("http_request", It.IsAny<double>()), Times.Once);
        _mockMetrics.Verify(m => m.RecordCount("http_requests", 1, 
            It.Is<(string Key, string Value)[]>(tags => 
                tags.Any(t => t.Key == "method" && t.Value == "GET") &&
                tags.Any(t => t.Key == "endpoint" && t.Value == "/api/test") &&
                tags.Any(t => t.Key == "status" && t.Value == "success"))), Times.Once);
    }

    [Fact]
    public async Task Exception_records_error_metrics()
    {
        // Arrange
        var context = CreateHttpContext("/api/error");
        var sut = CreateMiddleware(_ => throw new NotFoundException("Test", 1));

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(404);

        _mockMetrics.Verify(m => m.RecordRequest("http_request", "error"), Times.Once);
        _mockMetrics.Verify(m => m.RecordDuration("http_request", It.IsAny<double>()), Times.Once);
        _mockMetrics.Verify(m => m.RecordCount("http_requests", 1, 
            It.Is<(string Key, string Value)[]>(tags => 
                tags.Any(t => t.Key == "status" && t.Value == "error") &&
                tags.Any(t => t.Key == "status_code" && t.Value == "404"))), Times.Once);
        _mockMetrics.Verify(m => m.RecordCount("http_errors", 1, 
            It.Is<(string Key, string Value)[]>(tags => 
                tags.Any(t => t.Key == "error_type" && t.Value == "not_found") &&
                tags.Any(t => t.Key == "exception_type" && t.Value == "NotFoundException"))), Times.Once);
    }

    [Theory]
    [InlineData(typeof(FluentValidation.ValidationException), "validation_error", 422)]
    [InlineData(typeof(DomainException), "domain_error", 400)] 
    [InlineData(typeof(InvalidOperationException), "internal_error", 500)]
    public async Task Different_exception_types_record_appropriate_error_metrics(Type exceptionType, string expectedErrorType, int expectedStatusCode)
    {
        // Arrange
        var context = CreateHttpContext("/api/test");
        Exception exception = exceptionType.Name switch
        {
            nameof(FluentValidation.ValidationException) => new FluentValidation.ValidationException([new ValidationFailure("Test", "Test error")]),
            nameof(DomainException) => new DomainException("Test domain error"),
            nameof(InvalidOperationException) => new InvalidOperationException("Test internal error"),
            _ => throw new ArgumentException($"Unsupported exception type: {exceptionType}")
        };

        var sut = CreateMiddleware(_ => throw exception);

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(expectedStatusCode);

        _mockMetrics.Verify(m => m.RecordCount("http_errors", 1, 
            It.Is<(string Key, string Value)[]>(tags => 
                tags.Any(t => t.Key == "error_type" && t.Value == expectedErrorType) &&
                tags.Any(t => t.Key == "exception_type" && t.Value == exceptionType.Name) &&
                tags.Any(t => t.Key == "status_code" && t.Value == expectedStatusCode.ToString()))), Times.Once);
    }
}