using FluentValidation;
using KeeperData.Application.Configuration;
using KeeperData.Application.Queries.Countries.Adapters;
using KeeperData.Application.Queries.Cphs.Adapters;
using KeeperData.Application.Queries.Parties.Adapters;
using KeeperData.Application.Queries.Sites.Adapters;
using KeeperData.Application.Services.UserAccounts;
using KeeperData.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeeperData.Application.Setup;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IRequestExecutor).Assembly);
        });

        services.AddScoped<IRequestExecutor, RequestExecutor>();
        RegisterValidationConfig(configuration, services);

        services.AddValidatorsFromAssemblyContaining<IRequestExecutor>();

        services.AddScoped<CountriesQueryAdapter>();
        services.AddScoped<CphsQueryAdapter>();
        services.AddScoped<SitesQueryAdapter>();
        services.AddScoped<PartiesQueryAdapter>();

        services.Configure<UserAccountAssociationConfig>(configuration.GetSection(UserAccountAssociationConfig.SectionName));
        services.AddScoped<IUserAccountAssociationBuilder, UserAccountAssociationBuilder>();
    }

    /// <summary>
    /// Register strongly-typed config for each query validator.
    /// </summary>
    /// <remarks>Each validator constructor can request a strongly typed config for its own particular defaults
    /// (e.g. GetSiteQueryValidator constructor takes parameter of type QueryValidationConfig&lt;GetSiteQueryValidator&gt;).</remarks>
    private static void RegisterValidationConfig(IConfiguration configuration, IServiceCollection services)
    {
        var queryValidationConfig = configuration.GetSection(QueryValidationConfig.SectionName).Get<List<QueryValidationConfig>>();
        var validatorTypes = typeof(Queries.Sites.GetSitesQueryValidator).Assembly.GetTypes();
        var getConfigSectionMethod = typeof(ConfigurationBinder)
            .GetMethods()
            .Single(m => m.Name == "Get"
                && m.ContainsGenericParameters
                && m.GetParameters().Length == 1
                && m.GetParameters().Single().ParameterType == typeof(IConfiguration));

        for (var i = 0; i < queryValidationConfig?.Count; i++)
        {
            var validatorType = validatorTypes.Single(t => t.Name == queryValidationConfig[i].ValidatorType);
            var typeOfConfigForValidator = typeof(QueryValidationConfig<>).MakeGenericType(validatorType);
            var configInstance = getConfigSectionMethod
                .MakeGenericMethod(typeOfConfigForValidator)
                .Invoke(null, [configuration.GetSection($"{QueryValidationConfig.SectionName}:{i}")]);
            services.AddSingleton(typeOfConfigForValidator, configInstance!);
        }
    }
}