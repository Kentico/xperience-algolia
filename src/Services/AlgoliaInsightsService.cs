using Algolia.Search.Clients;

using CMS.ContactManagement;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Models;

using System;

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
        private readonly AlgoliaInsightsOptions mOptions;


        public override string ParameterNameObjectId => "object";


        public override string ParameterNameQueryId => "query";


        public override string ParameterNameIndexName => "index";


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


        private string IndexName
        {
            get
            {
                return QueryHelper.GetString(ParameterNameIndexName, "");
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
        public AlgoliaInsightsService(IInsightsClient insightsClient, AlgoliaInsightsOptions options)
        {
            mInsightsClient = insightsClient;
            mOptions = options;
        }


        public override void LogSearchResultClicked()
        {
            if(mOptions.TrackSearchResultClicks)
            {
                if (!String.IsNullOrEmpty(ContactGUID) && !String.IsNullOrEmpty(ObjectId) && !String.IsNullOrEmpty(IndexName) && !String.IsNullOrEmpty(QueryId) && Position > 0)
                {
                    mInsightsClient.User(ContactGUID).ClickedObjectIDsAfterSearch(mOptions.SearchResultClickedEventName, IndexName, new string[] { ObjectId }, new uint[] { Position }, QueryId);
                }
            }

            if (mOptions.TrackSearchResultConversions)
            {
                if (!String.IsNullOrEmpty(ContactGUID) && !String.IsNullOrEmpty(ObjectId) && !String.IsNullOrEmpty(IndexName) && !String.IsNullOrEmpty(QueryId))
                {
                    mInsightsClient.User(ContactGUID).ConvertedObjectIDsAfterSearch(mOptions.SearchResultConversionEventName, IndexName, new string[] { ObjectId }, QueryId);
                }
            }
        }
    }
}
