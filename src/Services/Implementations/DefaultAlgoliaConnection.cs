using Algolia.Search.Clients;

using CMS;
using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Services;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: RegisterImplementation(typeof(IAlgoliaConnection), typeof(DefaultAlgoliaConnection), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaConnection"/>.
    /// </summary>
    public class DefaultAlgoliaConnection : IAlgoliaConnection
    {
        private string indexName;
        private Type searchModelType;
        private SearchIndex searchIndex;
        private readonly ISearchClient searchClient;
        private readonly IEventLogService eventLogService;
        private readonly IAlgoliaRegistrationService algoliaRegistrationService;


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAlgoliaConnection"/> class.
        /// </summary>
        public DefaultAlgoliaConnection(ISearchClient searchClient,
            IEventLogService eventLogService,
            IAlgoliaRegistrationService algoliaRegistrationService)
        {
            this.searchClient = searchClient;
            this.eventLogService = eventLogService;
            this.algoliaRegistrationService = algoliaRegistrationService;
        }

        
        public void Initialize(string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(indexName);
            }

            this.indexName = indexName;
            searchIndex = searchClient.InitIndex(indexName);
            searchModelType = algoliaRegistrationService.GetModelByIndexName(indexName);

            if (searchModelType == null)
            {
                throw new InvalidOperationException($"Unable to load search model class for index '{indexName}.'");
            }

            if (!searchModelType.IsSubclassOf(typeof(AlgoliaSearchModel)))
            {
                throw new InvalidOperationException($"Algolia search models must extend the {nameof(AlgoliaSearchModel)} class.");
            }
        }


        public int DeleteRecords(IEnumerable<string> objectIds)
        {
            var deletedCount = 0;
            if (objectIds == null || objectIds.Count() == 0)
            {
                return 0;
            }

            var responses = searchIndex.DeleteObjects(objectIds).Responses;
            foreach (var response in responses)
            {
                deletedCount += response.ObjectIDs.Count();
            }

            return deletedCount;
        }


        public int UpsertRecords(IEnumerable<JObject> dataObjects)
        {
            var upsertedCount = 0;
            if (dataObjects == null || dataObjects.Count() == 0)
            {
                return 0;
            }

            try
            {                
                var responses = searchIndex.SaveObjects(dataObjects).Responses;
                foreach (var response in responses)
                {
                    upsertedCount += response.ObjectIDs.Count();
                }

                return upsertedCount;
            }
            catch (ArgumentNullException ex)
            {
                eventLogService.LogError(nameof(DefaultAlgoliaConnection), nameof(UpsertRecords), ex.Message);
                return upsertedCount;
            }
        }


        public void Rebuild()
        {
            if (searchModelType == null)
            {
                throw new InvalidOperationException("No registered search model class found for index.");
            }

            searchIndex.ClearObjects();

            var indexedNodes = new List<TreeNode>();
            var includedPathAttributes = searchModelType.GetCustomAttributes<IncludedPathAttribute>(false);
            foreach (var includedPathAttribute in includedPathAttributes)
            {
                var query = new MultiDocumentQuery()
                    .OnCurrentSite()
                    .Path(includedPathAttribute.AliasPath)
                    .PublishedVersion()
                    .WithCoupledColumns();

                if (includedPathAttribute.PageTypes.Length > 0)
                {
                    query.Types(includedPathAttribute.PageTypes);
                }

                if (includedPathAttribute.Cultures.Length > 0)
                {
                    query.Culture(includedPathAttribute.Cultures);
                }

                indexedNodes.AddRange(query.TypedResult);
            }

            AlgoliaQueueWorker.EnqueueAlgoliaQueueItems(indexedNodes.Select(node =>
                new AlgoliaQueueItem
                {
                    IndexName = indexName,
                    Node = node,
                    Deleted = false
                }
            ));
        }
    }
}