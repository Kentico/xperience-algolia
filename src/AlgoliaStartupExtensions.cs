using Algolia.Search.Clients;

using Kentico.Xperience.AlgoliaSearch.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        /// <param name="configuration">The application configuration.</param>
        public static IServiceCollection AddAlgolia(this IServiceCollection services, IConfiguration configuration)
        {
            var algoliaOptions = configuration.GetSection(AlgoliaOptions.SECTION_NAME).Get<AlgoliaOptions>();
            var insightsClient = new InsightsClient(algoliaOptions.ApplicationId, algoliaOptions.ApiKey);
            var searchClient = new SearchClient(algoliaOptions.ApplicationId, algoliaOptions.ApiKey);

            services.AddSingleton<IInsightsClient>(insightsClient);
            services.AddSingleton<ISearchClient>(searchClient);

            return services;
        }
    }
}