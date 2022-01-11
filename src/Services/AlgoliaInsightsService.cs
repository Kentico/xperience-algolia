using Algolia.Search.Clients;

using CMS.ContactManagement;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Models.Facets;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaInsightsService"/> which logs
    /// Algolia Insights events using the <see cref="ContactInfo.ContactGUID"/>
    /// as the user's identifier.
    /// </summary>
    public class AlgoliaInsightsService : IAlgoliaInsightsService
    {
        private readonly IInsightsClient mInsightsClient;


        public override string ParameterNameObjectId => "object";


        public override string ParameterNameQueryId => "query";


        public override string ParameterNamePosition => "pos";


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
        /// Constructor.
        /// </summary>
        public AlgoliaInsightsService(IInsightsClient insightsClient)
        {
            mInsightsClient = insightsClient;
        }


        public override void LogSearchResultClicked(string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(ObjectId) || String.IsNullOrEmpty(QueryId) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(eventName) || Position <= 0)
            {
                return;
            }

            mInsightsClient.User(ContactGUID).ClickedObjectIDsAfterSearch(eventName, indexName, new string[] { ObjectId }, new uint[] { Position }, QueryId);
        }


        public override void LogSearchResultConversion(string conversionName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(ObjectId) || String.IsNullOrEmpty(QueryId) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(conversionName))
            {
                return;
            }

            mInsightsClient.User(ContactGUID).ConvertedObjectIDsAfterSearch(conversionName, indexName, new string[] { ObjectId }, QueryId);
        }


        public override void LogPageViewed(int documentId, string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(eventName) || documentId <= 0)
            {
                return;
            }

            mInsightsClient.User(ContactGUID).ViewedObjectIDs(eventName, indexName, new string[] { documentId.ToString() });
        }


        public override void LogPageConversion(int documentId, string conversionName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(conversionName) || documentId <= 0)
            {
                return;
            }

            mInsightsClient.User(ContactGUID).ConvertedObjectIDs(conversionName, indexName, new string[] { documentId.ToString() });
        }


        public override void LogFacetsViewed(IEnumerable<AlgoliaFacetedAttribute> facets, string eventName, string indexName)
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
                mInsightsClient.User(ContactGUID).ViewedFilters(eventName, indexName, viewedFacets);
            }
        }


        public override void LogFacetClicked(string facet, string eventName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(facet) || String.IsNullOrEmpty(eventName) || String.IsNullOrEmpty(indexName))
            {
                return;
            }

            mInsightsClient.User(ContactGUID).ClickedFilters(eventName, indexName, new string[] { facet });
        }


        public override void LogFacetConverted(string facet, string conversionName, string indexName)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(facet) || String.IsNullOrEmpty(conversionName) || String.IsNullOrEmpty(indexName))
            {
                return;
            }

            mInsightsClient.User(ContactGUID).ConvertedFilters(conversionName, indexName, new string[] { facet });
        }
    }
}
