namespace KeeperData.Api;

using KeeperData.Api.Setup;
using KeeperData.Api.Utils;
using KeeperData.Infrastructure.Authentication.Setup;
using KeeperData.Infrastructure.Telemetry.Logging;
using Serilog;
using System.Diagnostics.CodeAnalysis;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = CreateBuilder(args);
        ConfigureBuilder(builder);

        var app = builder.Build();
        app.ConfigureRequestPipeline();
        app.Run();
    }

    public static WebApplicationBuilder CreateBuilder(string[] args)
        => WebApplication.CreateBuilder(args);

    [ExcludeFromCodeCoverage]
    public static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        builder.Services.AddCustomTrustStore();
        builder.Services.AddHttpContextAccessor();
        builder.Host.UseSerilog(SerilogLoggingExtensions.AddLogging);

        builder.Services
            .AddHttpClient("DefaultClient")
            .AddHeaderPropagation();

        builder.Services.AddTransient<ProxyHttpMessageHandler>();
        builder.Services
            .AddHttpClient("proxy")
            .ConfigurePrimaryHttpMessageHandler<ProxyHttpMessageHandler>();

        builder.Services.AddHeaderPropagation(options =>
        {
            var traceHeader = builder.Configuration.GetValue<string>("TraceHeader");
            if (!string.IsNullOrWhiteSpace(traceHeader))
                options.Headers.Add(traceHeader);
        });

        builder.Services.AddHostedService<Infrastructure.Services.MongoDataSeeder>();

        builder.Services.ConfigureApi(builder.Configuration);

        builder.Services.AddAuthenticationDependencies();
    }
}