using Algolia.Search.Models.Search;

using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Models.Facets;

using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Contains methods for logging Algolia Insights events.
    /// </summary>
    /// <remarks>See <see href="https://www.algolia.com/doc/guides/getting-analytics/search-analytics/advanced-analytics/"/></remarks>
    public interface IAlgoliaInsightsService
    {
        /// <summary>
        /// The parameter name used to store the <see cref="AlgoliaSearchModel.ObjectID"/> that
        /// is added to <see cref="AlgoliaSearchModel.Url"/> by <see cref="SetInsightsUrls"/>.
        /// </summary>
        public string ParameterNameObjectId
        {
            get;
        }


        /// <summary>
        /// The parameter name used to store the <see cref="AlgoliaSearchModel.QueryID"/> that
        /// is added to <see cref="AlgoliaSearchModel.Url"/> by <see cref="SetInsightsUrls"/>.
        /// </summary>
        public string ParameterNameQueryId
        {
            get;
        }


        /// <summary>
        /// The parameter name used to store the <see cref="AlgoliaSearchModel.Position"/> that
        /// is added to <see cref="AlgoliaSearchModel.Url"/> by <see cref="SetInsightsUrls"/>.
        /// </summary>
        public string ParameterNamePosition
        {
            get;
        }


        /// <summary>
        /// Logs a search result click event. Required query parameters must be present in the
        /// request, or no event is logged.
        /// </summary>
        public void LogSearchResultClicked(string eventName, string indexName);


        /// <summary>
        /// Logs a search result click conversion. Required query parameters must be present in the
        /// request, or no event is logged.
        /// </summary>
        public void LogSearchResultConversion(string conversionName, string indexName);


        /// <summary>
        /// Logs a conversion that didn't occur after an Algolia search.
        /// </summary>
        /// <param name="documentId">The <see cref="TreeNode.DocumentID"/> page that the conversion
        /// occurred on.</param>
        /// <param name="conversionName">The name of the conversion.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public void LogPageConversion(int documentId, string conversionName, string indexName);


        /// <summary>
        /// Logs an event when a visitor views a page contained within the Algolia index, but not after
        /// a search.
        /// </summary>
        /// <param name="documentId">>The <see cref="TreeNode.DocumentID"/> page that the conversion
        /// occurred on.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public void LogPageViewed(int documentId, string eventName, string indexName);


        /// <summary>
        /// Logs an event when a visitor views search facets but didn't click on them.
        /// </summary>
        /// <param name="facets">The facets that were displayed to the visitor.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public void LogFacetsViewed(IEnumerable<AlgoliaFacetedAttribute> facets, string eventName, string indexName);


        /// <summary>
        /// Logs an event when a visitor clicks a facet.
        /// </summary>
        /// <param name="facet">The facet name and value, e.g. "CoffeeIsDecaf:true."</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public void LogFacetClicked(string facet, string eventName, string indexName);


        /// <summary>
        /// Logs a conversion when a visitor clicks a facet.
        /// </summary>
        /// <param name="facet">The facet name and value, e.g. "CoffeeIsDecaf:true."</param>
        /// <param name="conversionName">The name of the conversion.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public void LogFacetConverted(string facet, string conversionName, string indexName);


        /// <summary>
        /// Updates the <see cref="AlgoliaSearchModel.Url"/> property of all search results
        /// with the query parameters needed to track search result click and conversion events.
        /// </summary>
        /// <typeparam name="TModel">The type of the Algolia search model.</typeparam>
        /// <param name="searchResponse">The full response of an Algolia search.</param>
        public void SetInsightsUrls<TModel>(SearchResponse<TModel> searchResponse) where TModel : AlgoliaSearchModel;


        /// <summary>
        /// Gets the Algolia hit's absolute URL with the appropriate query string parameters
        /// populated to log search result click events.
        /// </summary>
        /// <typeparam name="TModel">The type of the Algolia search model.</typeparam>
        /// <param name="hit">The Aloglia hit to retrieve the URL for.</param>
        /// <param name="position">The position the <paramref name="hit"/> appeared in the
        /// search results.</param>
        /// <param name="queryId">The unique identifier of the Algolia query.</param>
        public string GetInsightsUrl<TModel>(TModel hit, int position, string queryId) where TModel : AlgoliaSearchModel;
    }
}
