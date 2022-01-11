using Algolia.Search.Models.Search;

using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Services;

namespace Kentico.Xperience.AlgoliaSearch.Helpers
{
    /// <summary>
    /// Methods for assisting with the logging of Algolia Insights events and conversions.
    /// </summary>
    public class AlgoliaInsightsHelper
    {
        /// <summary>
        /// Updates the <see cref="AlgoliaSearchModel.Url"/> property of all search results
        /// with the query parameters needed to track search result click and conversion events
        /// via the <see cref="AlgoliaInsightsService"/>.
        /// </summary>
        public static void SetInsightsUrls<TModel>(SearchResponse<TModel> searchResponse) where TModel : AlgoliaSearchModel
        {
            for (var i = 0; i < searchResponse.Hits.Count; i++)
            {
                var position = i + 1 + (searchResponse.HitsPerPage * (searchResponse.Page - 1));
                searchResponse.Hits[i].Url = GetInsightsUrl(searchResponse.Hits[i], position, searchResponse.QueryID);
            }
        }


        private static string GetInsightsUrl<TModel>(TModel hit, int position, string queryId) where TModel : AlgoliaSearchModel
        {
            var indexName = "";
            foreach (var index in AlgoliaSearchHelper.RegisteredIndexes)
            {
                if (index.Value == typeof(TModel))
                {
                    indexName = index.Key;
                }
            }

            if(string.IsNullOrEmpty(indexName))
            {
                return hit.Url;
            }

            var url = hit.Url;
            var insightsService = Service.Resolve<IAlgoliaInsightsService>();
            url = URLHelper.AddParameterToUrl(url, insightsService.ParameterNameObjectId, hit.ObjectID);
            url = URLHelper.AddParameterToUrl(url, insightsService.ParameterNamePosition, position.ToString());
            url = URLHelper.AddParameterToUrl(url, insightsService.ParameterNameQueryId, queryId);

            return url;
        }
    }
}
