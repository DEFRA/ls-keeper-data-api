using FluentValidation;
using KeeperData.Application.Orchestration;
using KeeperData.Application.Orchestration.Sam.Inserts;
using KeeperData.Application.Orchestration.Sam.Inserts.Steps;
using KeeperData.Application.Queries.Sites.Adapters;
using KeeperData.Application.Services;
using KeeperData.Core.Attributes;
using KeeperData.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace KeeperData.Application.Setup;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationLayer(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IRequestExecutor).Assembly);
        });

        services.AddScoped<IRequestExecutor, RequestExecutor>();
        services.AddValidatorsFromAssemblyContaining<IRequestExecutor>();

        services.AddScoped<SitesQueryAdapter>();

        RegisterOrchestrators(services, typeof(SamHoldingInsertOrchestrator).Assembly);
        RegisterSteps(services, typeof(SamHoldingInsertAggregationStep).Assembly);
        RegisterLookupServices(services);
    }

    public static void RegisterOrchestrators(IServiceCollection services, Assembly assembly)
    {
        var orchestratorTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.BaseType?.IsGenericType == true &&
                        t.BaseType.GetGenericTypeDefinition() == typeof(ImportOrchestrator<>));

        foreach (var orchestrator in orchestratorTypes)
        {
            services.AddScoped(orchestrator);
        }
    }

    public static void RegisterSteps(IServiceCollection services, Assembly assembly)
    {
        var stepTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IImportStep<>))
                .Select(i => new { Implementation = t, Service = i }));

        var orderedSteps = stepTypes
            .OrderBy(t => t.Implementation.GetCustomAttribute<StepOrderAttribute>()?.Order ?? int.MaxValue);

        foreach (var step in orderedSteps)
        {
            services.AddScoped(step.Service, step.Implementation);
        }
    }

    public static void RegisterLookupServices(IServiceCollection services)
    {
        services.AddTransient<IRoleTypeLookupService, RoleTypeLookupService>();
    }
}