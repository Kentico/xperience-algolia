using Kentico.Xperience.AlgoliaSearch.Services;

namespace Kentico.Xperience.AlgoliaSearch.Models
{
    /// <summary>
    /// Options used during the logging of Algolia Insights events.
    /// </summary>
    public class AlgoliaInsightsOptions
    {
        /// <summary>
        /// If true, <see cref="IAlgoliaInsightsService.LogSearchResultClicked"/> will
        /// log an event after a search result is clicked when the required query
        /// string parameters are present. False by default.
        /// </summary>
        /// <remarks>See <see href="https://www.algolia.com/doc/api-reference/api-methods/clicked-object-ids-after-search/"/></remarks>
        public bool TrackSearchResultClicks
        {
            get;
            set;
        } = false;


        /// <summary>
        /// If true, <see cref="IAlgoliaInsightsService.LogSearchResultClicked"/> will
        /// log a conversion after a search result is clicked when the required query
        /// string parameters are present. False by default.
        /// </summary>
        /// <remarks>See <see href="https://www.algolia.com/doc/api-reference/api-methods/converted-object-ids-after-search/"/></remarks>
        public bool TrackSearchResultConversions
        {
            get;
            set;
        } = false;


        /// <summary>
        /// The name of the Algolia Insights event logged when a search result
        /// is clicked.
        /// </summary>
        public string SearchResultClickedEventName
        {
            get;
            set;
        } = "Search result clicked";


        /// <summary>
        /// The name of the Algolia Insights conversion logged when a search result
        /// is clicked.
        /// </summary>
        public string SearchResultConversionEventName
        {
            get;
            set;
        } = "Search result converted";
    }
}
