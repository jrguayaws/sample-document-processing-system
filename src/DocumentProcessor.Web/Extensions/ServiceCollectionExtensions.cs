using Amazon;
using Amazon.BedrockRuntime;
using DocumentProcessor.Web.Configuration;
using DocumentProcessor.Web.Data;
using DocumentProcessor.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DocumentProcessor.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BedrockOptions>()
            .Bind(configuration.GetSection(BedrockOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddDocumentProcessing(
        this IServiceCollection services,
        DatabaseConnection connection)
    {
        // A factory rather than a scoped DbContext: Blazor Server circuits outlive a
        // request, and a shared context cannot serve overlapping renders.
        services.AddDbContextFactory<AppDbContext>(options => _ = connection.Info.Provider switch
        {
            DatabaseProvider.SqlServer => options.UseSqlServer(connection.ConnectionString),
            // Wiring PostgreSQL means adding Npgsql.EntityFrameworkCore.PostgreSQL and
            // calling UseNpgsql here. Fail loudly rather than hand Npgsql syntax to SqlClient.
            _ => throw new NotSupportedException(
                $"{connection.Info.DisplayName} is not wired up yet. Add the Npgsql EF Core provider and call UseNpgsql.")
        });
        services.AddSingleton(connection.Info);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAmazonBedrockRuntime>(provider =>
        {
            var region = provider.GetRequiredService<IOptions<BedrockOptions>>().Value.Region;
            return new AmazonBedrockRuntimeClient(new AmazonBedrockRuntimeConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(region)
            });
        });

        services.AddSingleton<IDocumentStorage, FileSystemDocumentStorage>();
        services.AddSingleton<DocumentTextExtractor>();
        services.AddSingleton<IDocumentSummarizer, BedrockDocumentSummarizer>();
        services.AddScoped<DocumentPipeline>();

        return services;
    }
}
