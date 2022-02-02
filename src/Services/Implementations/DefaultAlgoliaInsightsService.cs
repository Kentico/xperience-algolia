using Algolia.Search.Clients;
using Algolia.Search.Models.Search;

using CMS;
using CMS.ContactManagement;
using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Models.Facets;
using Kentico.Xperience.AlgoliaSearch.Services;

using System;
using System.Collections.Generic;
using System.Linq;

[assembly: RegisterImplementation(typeof(IAlgoliaInsightsService), typeof(DefaultAlgoliaInsightsService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaInsightsService"/> which logs
    /// Algolia Insights events using the <see cref="ContactInfo.ContactGUID"/>
    /// as the user's identifier.
    /// </summary>
    public class DefaultAlgoliaInsightsService : IAlgoliaInsightsService
    {
        private readonly IAlgoliaRegistrationService algoliaRegistrationService;
        private readonly IInsightsClient insightsClient;


        public string ParameterNameObjectId => "object";


        public string ParameterNameQueryId => "query";


        public string ParameterNamePosition => "pos";


        private string ContactGUID
        {
            get
            {
                var currentContact = ContactManagementContext.CurrentContact;
                if (currentContact == null)
                {
                    return string.Empty;
                }

                return currentContact.ContactGUID.ToString();
            }
        }


        private string ObjectId
        {
            get
            {
                return QueryHelper.GetString(ParameterNameObjectId, "");
            }
        }


        private string QueryId
        {
            get
            {
                return QueryHelper.GetString(ParameterNameQueryId, "");
            }
        }


        private uint Position
        {
            get
            {
                return (uint)QueryHelper.GetInteger(ParameterNamePosition, 0);
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAlgoliaInsightsService"/> class.
        /// </summary>
        public DefaultAlgoliaInsightsService(IAlgoliaRegistrationService algoliaRegistrationService, IInsightsClient insightsClient)
        {
            this.algoliaRegistrationService = algoliaRegistrationService;
            this.insightsClient = insightsClient;
        }


        public void LogSearchResultClicked(string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(ObjectId) || String.IsNullOrEmpty(QueryId) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(eventName) || Position <= 0)
            {
                return;
            }

            insightsClient.User(ContactGUID).ClickedObjectIDsAfterSearch(eventName, indexName, new string[] { ObjectId }, new uint[] { Position }, QueryId);
        }


        public void LogSearchResultConversion(string conversionName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(ObjectId) || String.IsNullOrEmpty(QueryId) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(conversionName))
            {
                return;
            }

            insightsClient.User(ContactGUID).ConvertedObjectIDsAfterSearch(conversionName, indexName, new string[] { ObjectId }, QueryId);
        }


        public void LogPageViewed(int documentId, string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(eventName) || documentId <= 0)
            {
                return;
            }

            insightsClient.User(ContactGUID).ViewedObjectIDs(eventName, indexName, new string[] { documentId.ToString() });
        }


        public void LogPageConversion(int documentId, string conversionName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(conversionName) || documentId <= 0)
            {
                return;
            }

            insightsClient.User(ContactGUID).ConvertedObjectIDs(conversionName, indexName, new string[] { documentId.ToString() });
        }


        public void LogFacetsViewed(IEnumerable<AlgoliaFacetedAttribute> facets, string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || facets == null)
            {
                return;
            }

            var viewedFacets = new List<string>();
            foreach(var facetedAttribute in facets)
            {
                viewedFacets.AddRange(facetedAttribute.Facets.Select(facet => $"{facet.Attribute}:{facet.Value}"));
            }

            if (viewedFacets.Count > 0)
            {
                insightsClient.User(ContactGUID).ViewedFilters(eventName, indexName, viewedFacets);
            }
        }


        public void LogFacetClicked(string facet, string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(facet) || String.IsNullOrEmpty(eventName) || String.IsNullOrEmpty(indexName))
            {
                return;
            }

            insightsClient.User(ContactGUID).ClickedFilters(eventName, indexName, new string[] { facet });
        }


        public void LogFacetConverted(string facet, string conversionName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(facet) || String.IsNullOrEmpty(conversionName) || String.IsNullOrEmpty(indexName))
            {
                return;
            }

            insightsClient.User(ContactGUID).ConvertedFilters(conversionName, indexName, new string[] { facet });
        }


        public void SetInsightsUrls<TModel>(SearchResponse<TModel> searchResponse) where TModel : AlgoliaSearchModel
        {
            for (var i = 0; i < searchResponse.Hits.Count; i++)
            {
                var position = i + 1 + (searchResponse.HitsPerPage * searchResponse.Page);
                searchResponse.Hits[i].Url = GetInsightsUrl(searchResponse.Hits[i], position, searchResponse.QueryID);
            }
        }


        public string GetInsightsUrl<TModel>(TModel hit, int position, string queryId) where TModel : AlgoliaSearchModel
        {
            var indexName = "";
            foreach (var index in algoliaRegistrationService.RegisteredIndexes)
            {
                if (index.Value == typeof(TModel))
                {
                    indexName = index.Key;
                }
            }

            if (string.IsNullOrEmpty(indexName))
            {
                return hit.Url;
            }

            var url = hit.Url;
            url = URLHelper.AddParameterToUrl(url, ParameterNameObjectId, hit.ObjectID);
            url = URLHelper.AddParameterToUrl(url, ParameterNamePosition, position.ToString());
            url = URLHelper.AddParameterToUrl(url, ParameterNameQueryId, queryId);

            return url;
        }
    }
}
