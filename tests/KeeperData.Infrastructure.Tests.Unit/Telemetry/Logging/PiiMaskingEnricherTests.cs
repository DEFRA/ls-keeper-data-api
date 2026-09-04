using Elastic.CommonSchema.Serilog;
using Elastic.Serilog.Enrichers.Web;
using FluentAssertions;
using KeeperData.Infrastructure.Telemetry.Logging;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace KeeperData.Infrastructure.Tests.Unit.Telemetry.Logging;

public class PiiMaskingEnricherTests
{
    private readonly PiiMaskingEnricher _enricher = new();

    [Fact]
    public void Enrich_MasksScalarEmailString()
    {
        var logEvent = CreateLogEvent(new LogEventProperty("RequestPath", new ScalarValue("/api/test?email=test@example.com")));

        _enricher.Enrich(logEvent, new PropertyFactory());

        var property = logEvent.Properties["RequestPath"] as ScalarValue;
        property.Should().NotBeNull();
        property!.Value.Should().Be("/api/test?email=***");
    }

    [Fact]
    public void Enrich_MasksNestedUrlInStructure()
    {
        var urlProperties = new[]
        {
            new LogEventProperty("full", new ScalarValue("http://localhost/cph-associations?email=secret@test.com")),
            new LogEventProperty("original", new ScalarValue("/cph-associations?email=secret@test.com")),
            new LogEventProperty("query", new ScalarValue("?email=secret@test.com"))
        };

        var urlStructure = new StructureValue(urlProperties);
        var httpContextProperties = new[]
        {
            new LogEventProperty("url", urlStructure)
        };
        var httpContextStructure = new StructureValue(httpContextProperties);

        var logEvent = CreateLogEvent(new LogEventProperty("RequestMetadata", httpContextStructure));

        _enricher.Enrich(logEvent, new PropertyFactory());

        var modifiedHttpContext = logEvent.Properties["RequestMetadata"] as StructureValue;
        modifiedHttpContext.Should().NotBeNull();
        
        var modifiedUrl = modifiedHttpContext!.Properties.Single(p => p.Name == "url").Value as StructureValue;
        modifiedUrl.Should().NotBeNull();

        var full = modifiedUrl!.Properties.Single(p => p.Name == "full").Value as ScalarValue;
        full!.Value.Should().Be("http://localhost/cph-associations?email=***");

        var original = modifiedUrl!.Properties.Single(p => p.Name == "original").Value as ScalarValue;
        original!.Value.Should().Be("/cph-associations?email=***");

        var query = modifiedUrl!.Properties.Single(p => p.Name == "query").Value as ScalarValue;
        query!.Value.Should().Be("?email=***");
    }

    [Fact]
    public void Enrich_RemovesTheRealEcsHttpContextPropertyWithoutMutatingTheRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("krds.example");
        context.Request.Path = "/cph-associations";
        context.Request.QueryString = new QueryString("?email=secret@example.com");

        var accessor = new HttpContextAccessor { HttpContext = context };
        var logEvent = CreateLogEvent();
        var ecsEnricher = new HttpContextEnricher(accessor);
        ecsEnricher.Enrich(logEvent, new PropertyFactory());

        logEvent.Properties.Should().ContainKey("HttpContext");

        new PiiMaskingEnricher(accessor).Enrich(logEvent, new PropertyFactory());

        logEvent.Properties.Should().NotContainKey("HttpContext");
        context.Request.QueryString.Value.Should().Be("?email=secret@example.com");

        using var output = new StringWriter();
        new EcsTextFormatter().Format(logEvent, output);
        output.ToString().Should().NotContain("secret@example.com");
    }

    [Fact]
    public void Enrich_KeepsEcsHttpContextForRequestsWithoutAnEmailQueryParameter()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?page=1");

        var accessor = new HttpContextAccessor { HttpContext = context };
        var logEvent = CreateLogEvent();
        new HttpContextEnricher(accessor).Enrich(logEvent, new PropertyFactory());

        new PiiMaskingEnricher(accessor).Enrich(logEvent, new PropertyFactory());

        logEvent.Properties.Should().ContainKey("HttpContext");
    }
    
    [Fact]
    public void Enrich_MasksInDictionary()
    {
        var elements = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>
        {
            new(new ScalarValue("RequestUrl"), new ScalarValue("https://api.example.com?email=hello@world.com"))
        };

        var dict = new DictionaryValue(elements);
        var logEvent = CreateLogEvent(new LogEventProperty("RequestData", dict));

        _enricher.Enrich(logEvent, new PropertyFactory());

        var modifiedDict = logEvent.Properties["RequestData"] as DictionaryValue;
        modifiedDict.Should().NotBeNull();
        
        var modifiedValue = modifiedDict!.Elements.Single().Value as ScalarValue;
        modifiedValue!.Value.Should().Be("https://api.example.com?email=***");
    }

    [Fact]
    public void Enrich_DoesNotModifySafeValues()
    {
        var logEvent = CreateLogEvent(new LogEventProperty("RequestPath", new ScalarValue("/api/test?user=123")));

        _enricher.Enrich(logEvent, new PropertyFactory());

        var property = logEvent.Properties["RequestPath"] as ScalarValue;
        property!.Value.Should().Be("/api/test?user=123");
    }

    private static LogEvent CreateLogEvent(params LogEventProperty[] properties)
    {
        return new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", []),
            properties);
    }

    private class PropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }

        public LogEventProperty CreateProperty(string name, LogEventPropertyValue value)
        {
            return new LogEventProperty(name, value);
        }
    }
}
