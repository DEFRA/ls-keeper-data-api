using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Infrastructure.ApiClients.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;
using Polly.Retry;
using System.Net;

namespace KeeperData.Infrastructure.ApiClients.Setup;

public static class ServiceCollectionExtensions
{
    public static void AddApiClientDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var apiClientConfigurations = configuration
            .GetSection("ApiClients")
            .Get<Dictionary<string, ApiClientConfiguration>>();

        if (apiClientConfigurations == null) return;

        services.AddSingleton(apiClientConfigurations);

        services.AddScoped<IDataBridgeClient, DataBridgeClient>();

        var healthChecksBuilder = services.AddHealthChecks();

        foreach (var (clientName, config) in apiClientConfigurations)
        {
            services.RegisterNamedHttpClient(
                clientName: clientName,
                resolveConfig: () => config,
                resiliencePolicy: config.ResiliencePolicy
            );

            if (config.HealthcheckEnabled)
            {
                healthChecksBuilder.Add(new HealthCheckRegistration(
                    name: $"http-client-{clientName}",
                    factory: sp => new ApiClientHealthCheck(sp.GetRequiredService<IHttpClientFactory>(), clientName),
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["http-client"]));
            }
        }
    }

    private static void RegisterNamedHttpClient(
        this IServiceCollection services,
        string clientName,
        Func<ApiClientConfiguration> resolveConfig,
        ResiliencePolicy resiliencePolicy)
    {
        var config = resolveConfig();

        services.AddHttpClient(clientName, client =>
        {
            client.BaseAddress = new Uri(config.BaseUrl!.TrimEnd('/'));
        })
            .AddResilienceHandler(clientName, (builder, context) =>
            {
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = resiliencePolicy.Retries,
                    Delay = TimeSpan.FromSeconds(resiliencePolicy.BaseDelaySeconds),
                    UseJitter = resiliencePolicy.UseJitter,
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = DefaultRetryPredicate
                });

                builder.AddTimeout(TimeSpan.FromSeconds(resiliencePolicy.TimeoutPeriodSeconds));
            });
    }

    private static ValueTask<bool> DefaultRetryPredicate(RetryPredicateArguments<HttpResponseMessage> args)
    {
        var response = args.Outcome.Result;
        if (response != null && response.StatusCode is >= HttpStatusCode.InternalServerError
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout)
        {
            return ValueTask.FromResult(true);
        }

        if (args.Outcome.Exception is HttpRequestException)
        {
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }
}