using Algolia.Search.Clients;

using Kentico.Xperience.AlgoliaSearch.Helpers;
using Kentico.Xperience.AlgoliaSearch.Services;

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
        /// Registers instances of <see cref="ISearchClient"/>, <see cref="IInsightsClient"/>,
        /// <see cref="IAlgoliaInsightsService"/> with Dependency Injection.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        public static void AddAlgolia(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var algoliaOptions = AlgoliaSearchHelper.GetAlgoliaOptions(configuration);
            services.AddSingleton<ISearchClient>(AlgoliaSearchHelper.GetSearchClient(configuration));
            services.AddSingleton<IInsightsClient>(new InsightsClient(algoliaOptions.ApplicationId, algoliaOptions.ApiKey));
            services.AddSingleton<IAlgoliaInsightsService, AlgoliaInsightsService>();
        }
    }
}