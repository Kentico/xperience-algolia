using Algolia.Search.Clients;

using Kentico.Xperience.AlgoliaSearch.Helpers;
using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Services;

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
        /// Registers instances of <see cref="ISearchClient"/>, <see cref="IInsightsClient"/>,
        /// <see cref="IAlgoliaInsightsService"/>, and <see cref="AlgoliaInsightsOptions"/> with
        /// Dependency Injection.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="insightsOptions">The Algolia Insights options to register. Properties
        /// of <see cref="AlgoliaInsightsOptions"/> that are not specified will use default values
        /// defined in that class.</param>
        public static void AddAlgolia(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<AlgoliaInsightsOptions> insightsOptions = null)
        {
            var algoliaOptions = AlgoliaSearchHelper.GetAlgoliaOptions(configuration);
            services.AddSingleton<ISearchClient>(AlgoliaSearchHelper.GetSearchClient(configuration));
            services.AddSingleton<IInsightsClient>(new InsightsClient(algoliaOptions.ApplicationId, algoliaOptions.ApiKey));
            services.AddSingleton<IAlgoliaInsightsService, AlgoliaInsightsService>();

            var defaultInsightsOptions = new AlgoliaInsightsOptions();
            if (insightsOptions is object)
            {
               insightsOptions(defaultInsightsOptions);
            }
            services.AddSingleton(defaultInsightsOptions);
        }
    }
}