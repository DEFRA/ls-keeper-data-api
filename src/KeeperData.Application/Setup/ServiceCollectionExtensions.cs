using FluentValidation;
using KeeperData.Application.Orchestration.ChangeScanning;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk;
using KeeperData.Application.Orchestration.ChangeScanning.Sam.Bulk.Steps;
using KeeperData.Application.Orchestration.Imports;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings;
using KeeperData.Application.Orchestration.Imports.Sam.Holdings.Steps;
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

        RegisterImportOrchestrators(services, typeof(SamHoldingImportOrchestrator).Assembly);
        RegisterImportSteps(services, typeof(SamHoldingImportAggregationStep).Assembly);
        RegisterScanOrchestrators(services, typeof(SamBulkScanOrchestrator).Assembly);
        RegisterScanSteps(services, typeof(SamHoldingBulkScanStep).Assembly);
        RegisterLookupServices(services);
    }

    public static void RegisterImportOrchestrators(IServiceCollection services, Assembly assembly)
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

    public static void RegisterImportSteps(IServiceCollection services, Assembly assembly)
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

    public static void RegisterScanOrchestrators(IServiceCollection services, Assembly assembly)
    {
        var orchestratorTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.BaseType?.IsGenericType == true &&
                        t.BaseType.GetGenericTypeDefinition() == typeof(ScanOrchestrator<>));

        foreach (var orchestrator in orchestratorTypes)
        {
            services.AddScoped(orchestrator);
        }
    }

    public static void RegisterScanSteps(IServiceCollection services, Assembly assembly)
    {
        var stepTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IScanStep<>))
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
        services.AddTransient<ICountryIdentifierLookupService, CountryIdentifierLookupService>();
        services.AddTransient<IPremiseActivityTypeLookupService, PremiseActivityTypeLookupService>();
        services.AddTransient<IPremiseTypeLookupService, PremiseTypeLookupService>();
        services.AddTransient<IProductionTypeLookupService, ProductionTypeLookupService>();
        services.AddTransient<IProductionUsageLookupService, ProductionUsageLookupService>();
        services.AddTransient<IRoleTypeLookupService, RoleTypeLookupService>();
        services.AddTransient<ISpeciesTypeLookupService, SpeciesTypeLookupService>();
        services.AddTransient<ISiteIdentifierTypeLookupService, SiteIdentifierTypeLookupService>();
    }
}