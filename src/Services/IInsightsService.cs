using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Helpers;
using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Models.Facets;

using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Contains methods for logging Algolia Insights events.
    /// </summary>
    /// <remarks>See <see href="https://www.algolia.com/doc/guides/getting-analytics/search-analytics/advanced-analytics/"/></remarks>
    public abstract class IAlgoliaInsightsService
    {
        /// <summary>
        /// The parameter name used to store the <see cref="AlgoliaSearchModel.ObjectID"/> that
        /// is added to <see cref="AlgoliaSearchModel.Url"/> by <see cref="AlgoliaInsightsHelper.UpdateInsightsProperties"/>.
        /// </summary>
        public abstract string ParameterNameObjectId
        {
            get;
        }


        /// <summary>
        /// The parameter name used to store the <see cref="AlgoliaSearchModel.QueryID"/> that
        /// is added to <see cref="AlgoliaSearchModel.Url"/> by <see cref="AlgoliaInsightsHelper.UpdateInsightsProperties"/>.
        /// </summary>
        public abstract string ParameterNameQueryId
        {
            get;
        }


        /// <summary>
        /// The parameter name used to store the <see cref="AlgoliaSearchModel.Position"/> that
        /// is added to <see cref="AlgoliaSearchModel.Url"/> by <see cref="AlgoliaInsightsHelper.UpdateInsightsProperties"/>.
        /// </summary>
        public abstract string ParameterNamePosition
        {
            get;
        }


        /// <summary>
        /// Logs a search result click event. Required query parameters must be present in the
        /// request, or no event is logged.
        /// </summary>
        public abstract void LogSearchResultClicked(string eventName, string indexName);


        /// <summary>
        /// Logs a search result click conversion. Required query parameters must be present in the
        /// request, or no event is logged.
        /// </summary>
        public abstract void LogSearchResultConversion(string conversionName, string indexName);


        /// <summary>
        /// Logs a conversion that didn't occur after an Algolia search.
        /// </summary>
        /// <param name="documentId">The <see cref="TreeNode.DocumentID"/> page that the conversion
        /// occurred on.</param>
        /// <param name="conversionName">The name of the conversion.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public abstract void LogPageConversion(int documentId, string conversionName, string indexName);


        /// <summary>
        /// Logs an event when a visitor views a page contained within the Algolia index, but not after
        /// a search.
        /// </summary>
        /// <param name="documentId">>The <see cref="TreeNode.DocumentID"/> page that the conversion
        /// occurred on.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public abstract void LogPageViewed(int documentId, string eventName, string indexName);


        /// <summary>
        /// Logs an event when a visitor views search facets but didn't click on them.
        /// </summary>
        /// <param name="facets">The facets that were displayed to the visitor.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public abstract void LogFacetsViewed(IEnumerable<AlgoliaFacetedAttribute> facets, string eventName, string indexName);


        /// <summary>
        /// Logs an event when a visitor clicks a facet.
        /// </summary>
        /// <param name="facet">The facet name and value, e.g. "coffeeIsDecaf:true."</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public abstract void LogFacetClicked(string facet, string eventName, string indexName);


        /// <summary>
        /// Logs a conversion when a visitor clicks a facet.
        /// </summary>
        /// <param name="facet">The facet name and value, e.g. "coffeeIsDecaf:true."</param>
        /// <param name="conversionName">The name of the conversion.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public abstract void LogFacetConverted(string facet, string conversionName, string indexName);
    }
}
