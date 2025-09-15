using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using KeeperData.Api.Tests.Integration.Consumers.Helpers;
using KeeperData.Core.Messaging.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KeeperData.Api.Tests.Integration.TestUtils;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public HttpClient? Client { get; private set; }
    public AWSOptions AwsOptions { get; private set; }

    public TestWebApplicationFactory()
    {
        var webApplicationBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "IntegrationTest"
        });

        var options = webApplicationBuilder.Configuration.GetAWSOptions();
        options.Credentials = new BasicAWSCredentials(
            webApplicationBuilder.Configuration["AWS_ACCESS_KEY_ID"],
            webApplicationBuilder.Configuration["AWS_SECRET_ACCESS_KEY"]
        );

        webApplicationBuilder.Services.Replace(new ServiceDescriptor(typeof(AWSOptions), options));
        webApplicationBuilder.Services.AddSingleton<IServer, TestServer>();

        AwsOptions = options;
        Client ??= CreateClient();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Replace(new ServiceDescriptor(typeof(AWSOptions), AwsOptions));

            services.AddSingleton<TestConsumerObserver<IntakeEventModel>>();
        });
        var app = builder.Build();
        app.Start();
        return app;
    }
}