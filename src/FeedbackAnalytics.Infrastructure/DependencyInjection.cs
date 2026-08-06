using FeedbackAnalytics.Domain.Contracts;
using FeedbackAnalytics.Infrastructure.Extractors;
using FeedbackAnalytics.Infrastructure.Options;
using FeedbackAnalytics.Infrastructure.Staging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FeedbackAnalytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CsvSourceOptions>(
            configuration.GetSection(CsvSourceOptions.SectionName));
        services.Configure<DatabaseSourceOptions>(
            configuration.GetSection(DatabaseSourceOptions.SectionName));
        services.Configure<ApiSourceOptions>(
            configuration.GetSection(ApiSourceOptions.SectionName));
        services.Configure<StagingOptions>(
            configuration.GetSection(StagingOptions.SectionName));

        services.AddSingleton(
            serviceProvider =>
            {
                IConfiguration appConfiguration =
                    serviceProvider.GetRequiredService<IConfiguration>();
                string connectionString =
                    appConfiguration.GetConnectionString("PostgreSql")
                    ?? throw new InvalidOperationException(
                        "ConnectionStrings:PostgreSql is required. Set the " +
                        "ConnectionStrings__PostgreSql environment variable.");

                return NpgsqlDataSource.Create(connectionString);
            });

        services.AddTransient<CsvExtractor>();
        services.AddTransient<DatabaseExtractor>();
        services.AddTransient<IExtractor>(
            serviceProvider => serviceProvider.GetRequiredService<CsvExtractor>());
        services.AddTransient<IExtractor>(
            serviceProvider => serviceProvider.GetRequiredService<DatabaseExtractor>());

        services
            .AddHttpClient<ApiExtractor>(
                (serviceProvider, client) =>
                {
                    ApiSourceOptions apiOptions =
                        serviceProvider.GetRequiredService<IOptions<ApiSourceOptions>>().Value;
                    client.BaseAddress = new Uri(apiOptions.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "FeedbackAnalyticsEtl/1.0");
                });
        services.AddTransient<IExtractor>(
            serviceProvider => serviceProvider.GetRequiredService<ApiExtractor>());

        services.AddSingleton<IStagingWriter, PostgresStagingWriter>();

        return services;
    }
}
