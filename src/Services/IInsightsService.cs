using Kentico.Xperience.AlgoliaSearch.Helpers;
using Kentico.Xperience.AlgoliaSearch.Models;

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
        /// The parameter name used to store the Algolia index name used during a search that
        /// is added to <see cref="AlgoliaSearchModel.Url"/> by <see cref="AlgoliaInsightsHelper.UpdateInsightsProperties"/>.
        /// </summary>
        public abstract string ParameterNameIndexName
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
        /// Logs a search result click event and conversion with Algolia Insights. Required query parameters
        /// must be present in the request, or no event is logged.
        /// </summary>
        public abstract void LogSearchResultClicked();
    }
}
