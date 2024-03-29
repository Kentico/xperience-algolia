﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Algolia.Search.Clients;
using Algolia.Search.Models.Insights;
using Algolia.Search.Models.Search;

using CMS.ContactManagement;
using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.Algolia.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Kentico.Xperience.Algolia.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaInsightsService"/> which logs
    /// Algolia Insights events using the <see cref="ContactInfo.ContactGUID"/>
    /// as the user's identifier.
    /// </summary>
    internal class DefaultAlgoliaInsightsService : IAlgoliaInsightsService
    {
        private readonly AlgoliaOptions algoliaOptions;
        private readonly IInsightsClient insightsClient;
        private readonly IEventLogService eventLogService;
        private readonly IHttpContextAccessor httpContextAccessor; 
        private readonly Regex queryParameterRegex = new Regex("^[a-fA-F0-9]{32}$");
        

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
                StringValues values;
                if (httpContextAccessor.HttpContext.Request.Query.TryGetValue(algoliaOptions.ObjectIdParameterName, out values))
                {
                    return values.FirstOrDefault();
                }

                return String.Empty;
            }
        }


        private string QueryId
        {
            get
            {
                StringValues values;
                if (httpContextAccessor.HttpContext.Request.Query.TryGetValue(algoliaOptions.QueryIdParameterName, out values))
                {
                    return values.FirstOrDefault();
                }

                return String.Empty;
            }
        }


        private uint Position
        {
            get
            {
                StringValues values;
                if (httpContextAccessor.HttpContext.Request.Query.TryGetValue(algoliaOptions.PositionParameterName, out values))
                {
                    return Convert.ToUInt32(values.FirstOrDefault());
                }

                return 0;
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAlgoliaInsightsService"/> class.
        /// </summary>
        public DefaultAlgoliaInsightsService(IOptions<AlgoliaOptions> algoliaOptions,
            IHttpContextAccessor httpContextAccessor,
            IInsightsClient insightsClient,
            IEventLogService eventLogService)
        {
            this.algoliaOptions = algoliaOptions.Value;
            this.httpContextAccessor = httpContextAccessor;
            this.insightsClient = insightsClient;
            this.eventLogService = eventLogService;
        }


        /// <inheritdoc />
        public async Task<InsightsResponse> LogSearchResultClicked(string eventName, string indexName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(ObjectId) || String.IsNullOrEmpty(QueryId) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(eventName) || Position <= 0)
            {
                return InvalidParameterResponse();
            }

            try
            {
                return await insightsClient.User(ContactGUID).ClickedObjectIDsAfterSearchAsync(eventName, indexName, new string[] { ObjectId }, new uint[] { Position }, QueryId, ct: cancellationToken);
            }
            catch (Exception ex)
            {
                eventLogService.LogException(nameof(DefaultAlgoliaInsightsService), nameof(LogSearchResultClicked), ex);
                return ExceptionResponse();
            }
        }


        /// <inheritdoc />
        public async Task<InsightsResponse> LogSearchResultConversion(string conversionName, string indexName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(ObjectId) || String.IsNullOrEmpty(QueryId) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(conversionName))
            {
                return InvalidParameterResponse();
            }

            try
            {
                return await insightsClient.User(ContactGUID).ConvertedObjectIDsAfterSearchAsync(conversionName, indexName, new string[] { ObjectId }, QueryId, ct: cancellationToken);
            }
            catch (Exception ex)
            {
                eventLogService.LogException(nameof(DefaultAlgoliaInsightsService), nameof(LogSearchResultConversion), ex);
                return ExceptionResponse();
            }
        }


        /// <inheritdoc />
        public async Task<InsightsResponse> LogPageViewed(int documentId, string eventName, string indexName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(eventName) || documentId <= 0)
            {
                return InvalidParameterResponse();
            }

            try
            {
                return await insightsClient.User(ContactGUID).ViewedObjectIDsAsync(eventName, indexName, new string[] { documentId.ToString() }, ct: cancellationToken);
            }
            catch (Exception ex)
            {
                eventLogService.LogException(nameof(DefaultAlgoliaInsightsService), nameof(LogPageViewed), ex);
                return ExceptionResponse();
            }
        }


        /// <inheritdoc />
        public async Task<InsightsResponse> LogPageConversion(int documentId, string conversionName, string indexName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(indexName) || String.IsNullOrEmpty(conversionName) || documentId <= 0)
            {
                return InvalidParameterResponse();
            }

            try
            {
                return await insightsClient.User(ContactGUID).ConvertedObjectIDsAsync(conversionName, indexName, new string[] { documentId.ToString() }, ct: cancellationToken);
            }
            catch (Exception ex)
            {
                eventLogService.LogException(nameof(DefaultAlgoliaInsightsService), nameof(LogPageConversion), ex);
                return ExceptionResponse();
            }
        }


        /// <inheritdoc />
        public async Task<InsightsResponse> LogFacetsViewed(IEnumerable<AlgoliaFacetedAttribute> facets, string eventName, string indexName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(ContactGUID) || facets == null)
            {
                return InvalidParameterResponse();
            }

            var viewedFacets = new List<string>();
            foreach(var facetedAttribute in facets)
            {
                viewedFacets.AddRange(facetedAttribute.Facets.Select(facet => $"{facet.Attribute}:{facet.Value}"));
            }

            if (viewedFacets.Count > 0)
            {
                try
                {
                    return await insightsClient.User(ContactGUID).ViewedFiltersAsync(eventName, indexName, viewedFacets, ct: cancellationToken);
                }
                catch (Exception ex)
                {
                    eventLogService.LogException(nameof(DefaultAlgoliaInsightsService), nameof(LogFacetsViewed), ex);
                    return ExceptionResponse();
                }
            }

            return new InsightsResponse()
            {
                Status = (int)HttpStatusCode.BadRequest,
                Message = "No facets were provided."
            };
        }


        /// <inheritdoc />
        public async Task<InsightsResponse> LogFacetClicked(string facet, string eventName, string indexName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(facet) || String.IsNullOrEmpty(eventName) || String.IsNullOrEmpty(indexName))
            {
                return InvalidParameterResponse();
            }

            try
            {
                return await insightsClient.User(ContactGUID).ClickedFiltersAsync(eventName, indexName, new string[] { facet }, ct: cancellationToken);
            }
            catch (Exception ex)
            {
                eventLogService.LogException(nameof(DefaultAlgoliaInsightsService), nameof(LogFacetClicked), ex);
                return ExceptionResponse();
            }
        }


        /// <inheritdoc />
        public async Task<InsightsResponse> LogFacetConverted(string facet, string conversionName, string indexName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(ContactGUID) || String.IsNullOrEmpty(facet) || String.IsNullOrEmpty(conversionName) || String.IsNullOrEmpty(indexName))
            {
                return InvalidParameterResponse();
            }

            try
            {
                return await insightsClient.User(ContactGUID).ConvertedFiltersAsync(conversionName, indexName, new string[] { facet }, ct: cancellationToken);
            }
            catch (Exception ex)
            {
                eventLogService.LogException(nameof(DefaultAlgoliaInsightsService), nameof(LogFacetConverted), ex);
                return ExceptionResponse();
            }
        }


        /// <inheritdoc />
        public void SetInsightsUrls<TModel>(SearchResponse<TModel> searchResponse) where TModel : AlgoliaSearchModel
        {
            for (var i = 0; i < searchResponse.Hits.Count; i++)
            {
                var position = i + 1 + (searchResponse.HitsPerPage * searchResponse.Page);
                searchResponse.Hits[i].Url = GetInsightsUrl(searchResponse.Hits[i], position, searchResponse.QueryID);
            }
        }


        private InsightsResponse ExceptionResponse()
        {
            return new InsightsResponse()
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Message = "Errors occurred while communicating with Algolia. Please check the Event Log for more details."
            };
        }


        /// <summary>
        /// Gets the Algolia hit's absolute URL with the appropriate query string parameters
        /// populated to log search result click events.
        /// </summary>
        /// <typeparam name="TModel">The type of the Algolia search model.</typeparam>
        /// <param name="hit">The Algolia hit to retrieve the URL for.</param>
        /// <param name="position">The position the <paramref name="hit"/> appeared in the
        /// search results.</param>
        /// <param name="queryId">The unique identifier of the Algolia query.</param>
        private string GetInsightsUrl<TModel>(TModel hit, int position, string queryId) where TModel : AlgoliaSearchModel
        {
            var url = hit.Url;
            url = URLHelper.AddParameterToUrl(url, algoliaOptions.ObjectIdParameterName, hit.ObjectID);
            url = URLHelper.AddParameterToUrl(url, algoliaOptions.PositionParameterName, position.ToString());
            if (queryParameterRegex.IsMatch(queryId))
            {
                url = URLHelper.AddParameterToUrl(url, algoliaOptions.QueryIdParameterName, queryId);
            }

            return url;
        }


        private InsightsResponse InvalidParameterResponse()
        {
            return new InsightsResponse()
            {
                Status = (int)HttpStatusCode.BadRequest,
                Message = "One or more parameters are invalid."
            };
        }
    }
}
