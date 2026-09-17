using KeeperData.Api.Setup;
using KeeperData.Api.Utils;
using KeeperData.Infrastructure.Telemetry.Logging;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using KeeperData.Infrastructure.Services;

var isOpenApiGeneration = string.Equals(
    Assembly.GetEntryAssembly()?.GetName().Name,
    "GetDocument.Insider",
    StringComparison.Ordinal);
var app = CreateWebApplication(args, isOpenApiGeneration);
await app.RunAsync();
return;

[ExcludeFromCodeCoverage]
static WebApplication CreateWebApplication(string[] args, bool isOpenApiGeneration)
{
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        ApplicationName = typeof(Program).Assembly.GetName().Name
    });

    if (isOpenApiGeneration)
    {
        builder.Services.ConfigureOpenApiGeneration();
    }
    else
    {
        ConfigureBuilder(builder);
    }

    var app = builder.Build();

    if (isOpenApiGeneration)
    {
        app.ConfigureOpenApiGenerationPipeline();
    }
    else
    {
        app.ConfigureRequestPipeline();
    }

    return app;
}

[ExcludeFromCodeCoverage]
static void ConfigureBuilder(WebApplicationBuilder builder)
{
    builder.Configuration.AddEnvironmentVariables();

    // Load certificates into Trust Store - Note must happen before Mongo and Http client connections.
    builder.Services.AddCustomTrustStore();

    // Configure logging to use the CDP Platform standards.
    builder.Services.AddHttpContextAccessor();
    builder.Host.UseSerilog(SerilogLoggingExtensions.AddLogging);

    // Default HTTP Client
    builder.Services
        .AddHttpClient("DefaultClient")
        .AddHeaderPropagation();

    // Proxy HTTP Client
    builder.Services.AddTransient<ProxyHttpMessageHandler>();
    builder.Services
        .AddHttpClient("proxy")
        .ConfigurePrimaryHttpMessageHandler<ProxyHttpMessageHandler>();

    // Propagate trace header.
    builder.Services.AddHeaderPropagation(options =>
    {
        var traceHeader = builder.Configuration.GetValue<string>("TraceHeader");
        if (!string.IsNullOrWhiteSpace(traceHeader))
        {
            options.Headers.Add(traceHeader);
        }
    });

    builder.Services.AddHostedService<MongoDataSeeder>();

    builder.Services.AddSingleton<KeeperData.Core.Services.IReferenceDataCache, ReferenceDataCache>();
    builder.Services.AddHostedService(sp => (ReferenceDataCache)sp.GetRequiredService<KeeperData.Core.Services.IReferenceDataCache>());

    builder.Services.ConfigureApi(builder.Configuration, builder.Environment);
}

public partial class Program { }