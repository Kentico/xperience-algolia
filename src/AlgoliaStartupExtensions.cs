using System;
using Algolia.Search.Clients;
using Kentico.Xperience.AlgoliaSearch.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Application startup extension methods.
    /// </summary>
    public static class AlgoliaStartupExtensions
    {
        /// <summary>
        /// Registers instances of <see cref="IInsightsClient"/> and <see cref="ISearchClient"/>
        /// with Dependency Injection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        public static IServiceCollection AddAlgolia(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AlgoliaOptions>(configuration.GetSection(AlgoliaOptions.SECTION_NAME));
            services.PostConfigure<AlgoliaOptions>(options =>
            {
                if (String.IsNullOrEmpty(options.ApplicationId) || String.IsNullOrEmpty(options.ApiKey))
                {
                    // Algolia configuration is not valid, but IEventLogService can't be resolved during startup.
                    // Set dummy values so that DI is not broken, but errors can be captured when attempting to use the client
                    options.ApplicationId = "NO_APP";
                    options.ApiKey = "NO_KEY";
                }
            });

            return services
                .AddSingleton<IInsightsClient>(s =>
                {
                    var options = s.GetRequiredService<IOptions<AlgoliaOptions>>();

                    return new InsightsClient(options.Value.ApplicationId, options.Value.ApiKey);
                })
                .AddSingleton<ISearchClient>(s =>
                {
                    var options = s.GetRequiredService<IOptions<AlgoliaOptions>>();

                    return new SearchClient(options.Value.ApplicationId, options.Value.ApiKey);
                });
        }
    }
}