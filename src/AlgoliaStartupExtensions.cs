using Algolia.Search.Clients;

using Kentico.Xperience.AlgoliaSearch.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System;

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
        /// <param name="services"></param>
        /// <param name="configuration">The application configuration.</param>
        public static IServiceCollection AddAlgolia(this IServiceCollection services, IConfiguration configuration)
        {
            var algoliaOptions = configuration.GetSection(AlgoliaOptions.SECTION_NAME).Get<AlgoliaOptions>();
            if (String.IsNullOrEmpty(algoliaOptions.ApplicationId) || String.IsNullOrEmpty(algoliaOptions.ApiKey))
            {
                // Algolia configuration is not valid, but IEventLogService can't be resolved during startup.
                // Set dummy values so that DI is not broken, but errors can be captured when attempting to use the client
                algoliaOptions.ApplicationId = "NO_APP";
                algoliaOptions.ApiKey = "NO_KEY";
            }

            var insightsClient = new InsightsClient(algoliaOptions.ApplicationId, algoliaOptions.ApiKey);
            var searchClient = new SearchClient(algoliaOptions.ApplicationId, algoliaOptions.ApiKey);

            services.AddSingleton<IInsightsClient>(insightsClient);
            services.AddSingleton<ISearchClient>(searchClient);

            return services;
        }
    }
}