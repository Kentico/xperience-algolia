using System;
using System.Collections.Generic;
using System.Linq;

using Algolia.Search.Clients;
using Algolia.Search.Models.Common;

using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.Helpers.Caching.Abstractions;
using CMS.WorkflowEngine;

using Kentico.Xperience.Algolia.Models;

using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.Algolia.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaClient"/>.
    /// </summary>
    internal class DefaultAlgoliaClient : IAlgoliaClient
    {
        private readonly IAlgoliaIndexService algoliaIndexService;
        private readonly IAlgoliaObjectGenerator algoliaObjectGenerator;
        private readonly ICacheAccessor cacheAccessor;
        private readonly IEventLogService eventLogService;
        private readonly IVersionHistoryInfoProvider versionHistoryInfoProvider;
        private readonly IWorkflowStepInfoProvider workflowStepInfoProvider;
        private readonly IProgressiveCache progressiveCache;
        private readonly ISearchClient searchClient;
        private const string CACHEKEY_STATISTICS = "Algolia|ListIndices";


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAlgoliaClient"/> class.
        /// </summary>
        public DefaultAlgoliaClient(IAlgoliaIndexService algoliaIndexService,
            IAlgoliaObjectGenerator algoliaObjectGenerator,
            ICacheAccessor cacheAccessor,
            IEventLogService eventLogService,
            IVersionHistoryInfoProvider versionHistoryInfoProvider,
            IWorkflowStepInfoProvider workflowStepInfoProvider,
            IProgressiveCache progressiveCache,
            ISearchClient searchClient)
        {
            this.algoliaIndexService = algoliaIndexService;
            this.algoliaObjectGenerator = algoliaObjectGenerator;
            this.cacheAccessor = cacheAccessor;
            this.eventLogService = eventLogService;
            this.versionHistoryInfoProvider = versionHistoryInfoProvider;
            this.workflowStepInfoProvider = workflowStepInfoProvider;
            this.progressiveCache = progressiveCache;
            this.searchClient = searchClient;
        }


        /// <inheritdoc />
        public int DeleteRecords(IEnumerable<string> objectIds, string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            if (objectIds == null || !objectIds.Any())
            {
                return 0;
            }

            var deletedCount = 0;
            var searchIndex = algoliaIndexService.InitializeIndex(indexName);
            var batchIndexingResponse = searchIndex.DeleteObjects(objectIds);
            foreach (var response in batchIndexingResponse.Responses)
            {
                deletedCount += response.ObjectIDs.Count();
            }

            return deletedCount;
        }


        /// <inheritdoc/>
        public ICollection<IndicesResponse> GetStatistics()
        {
            return progressiveCache.Load((cs) =>
            {
                var response = searchClient.ListIndices();
                return response.Items;
            }, new CacheSettings(20, CACHEKEY_STATISTICS));
        }


        /// <inheritdoc />
        public int ProcessAlgoliaTasks(IEnumerable<AlgoliaQueueItem> items)
        {
            var successfulOperations = 0;

            // Group queue items based on index name
            var groups = items.GroupBy(item => item.IndexName);
            foreach (var group in groups)
            {
                try
                {
                    var deleteIds = new List<string>();
                    var deleteTasks = group.Where(queueItem => queueItem.TaskType == AlgoliaTaskType.DELETE);
                    deleteIds.AddRange(GetIdsToDelete(group.Key, deleteTasks));

                    var updateTasks = group.Where(queueItem => queueItem.TaskType == AlgoliaTaskType.UPDATE || queueItem.TaskType == AlgoliaTaskType.CREATE);
                    var upsertData = new List<JObject>();
                    foreach (var queueItem in updateTasks)
                    {
                        // There may be less fragments than previously indexed. Delete fragments created by the
                        // previous version of the node
                        deleteIds.AddRange(GetFragmentsToDelete(queueItem));
                        var data = GetDataToUpsert(queueItem);
                        upsertData.AddRange(data);
                    }

                    successfulOperations += DeleteRecords(deleteIds, group.Key);
                    successfulOperations += UpsertRecords(upsertData, group.Key);
                }
                catch (Exception ex)
                {
                    eventLogService.LogError(nameof(DefaultAlgoliaClient), nameof(ProcessAlgoliaTasks), ex.Message);
                }
            }

            return successfulOperations;
        }


        /// <inheritdoc />
        public void Rebuild(string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            var algoliaIndex = IndexStore.Instance.Get(indexName);
            if (algoliaIndex == null)
            {
                throw new InvalidOperationException($"The index '{indexName}' is not registered.");
            }

            // Clear statistics cache so listing displays updated data after rebuild
            cacheAccessor.Remove(CACHEKEY_STATISTICS);

            var indexedNodes = new List<TreeNode>();
            foreach (var includedPathAttribute in algoliaIndex.IncludedPaths)
            {
                var query = new MultiDocumentQuery()
                    .Path(includedPathAttribute.AliasPath)
                    .PublishedVersion()
                    .WithCoupledColumns();

                if (includedPathAttribute.PageTypes.Any())
                {
                    query.Types(includedPathAttribute.PageTypes);
                }

                if (includedPathAttribute.Cultures.Any())
                {
                    query.Culture(includedPathAttribute.Cultures);
                }

                if (algoliaIndex.SiteNames.Any())
                {
                    foreach (var site in algoliaIndex.SiteNames)
                    {
                        query.OnSite(site).Or();
                    }
                }

                indexedNodes.AddRange(query.TypedResult);
            }

            var dataToUpsert = new List<JObject>();
            indexedNodes.ForEach(node => dataToUpsert.AddRange(GetDataToUpsert(new AlgoliaQueueItem(node, AlgoliaTaskType.CREATE, algoliaIndex.IndexName))));
            var searchIndex = algoliaIndexService.InitializeIndex(algoliaIndex.IndexName);
            searchIndex.ClearObjects();
            searchIndex.SaveObjects(dataToUpsert);
        }


        /// <inheritdoc />
        public int UpsertRecords(IEnumerable<JObject> dataObjects, string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            if (dataObjects == null || !dataObjects.Any())
            {
                return 0;
            }

            var upsertedCount = 0;
            var searchIndex = algoliaIndexService.InitializeIndex(indexName);
            var batchIndexingResponse = searchIndex.PartialUpdateObjects(dataObjects, createIfNotExists: true);
            foreach (var response in batchIndexingResponse.Responses)
            {
                upsertedCount += response.ObjectIDs.Count();
            }

            return upsertedCount;
        }


        /// <summary>
        /// Gets the IDs of the fragments previously generated by a node update. Because the data that was split could
        /// be smaller than previous updates, if they were not deleted during an update task, there would be orphaned
        /// data in Algolia. When the <see cref="AlgoliaQueueItem.TaskType"/> is <see cref="AlgoliaTaskType.UPDATE"/>,
        /// we must check for a previous version and delete the fragments generated by that version, before upserting new fragments.
        /// </summary>
        /// <param name="queueItem">The item being processed.</param>
        /// <returns>A list of Algolia IDs that should be deleted, or an empty list.</returns>
        /// <exception cref="ArgumentNullException" />
        private IEnumerable<string> GetFragmentsToDelete(AlgoliaQueueItem queueItem)
        {
            var algoliaIndex = IndexStore.Instance.Get(queueItem.IndexName);
            if (queueItem.TaskType != AlgoliaTaskType.UPDATE || algoliaIndex.DistinctOptions == null)
            {
                // Only split data on UPDATE tasks if splitting is enabled
                return Enumerable.Empty<string>();
            }

            var publishedStepId = workflowStepInfoProvider.Get()
                .TopN(1)
                .WhereEquals(nameof(WorkflowStepInfo.StepWorkflowID), queueItem.Node.WorkflowStep.StepWorkflowID)
                .WhereEquals(nameof(WorkflowStepInfo.StepType), WorkflowStepTypeEnum.DocumentPublished)
                .AsIDQuery()
                .GetScalarResult<int>(0);
            var previouslyPublishedVersionID = versionHistoryInfoProvider.Get()
                .TopN(1)
                .WhereEquals(nameof(VersionHistoryInfo.DocumentID), queueItem.Node.DocumentID)
                .WhereEquals(nameof(VersionHistoryInfo.NodeSiteID), queueItem.Node.NodeSiteID)
                .WhereEquals(nameof(VersionHistoryInfo.VersionWorkflowStepID), publishedStepId)
                .OrderByDescending(nameof(VersionHistoryInfo.WasPublishedTo))
                .AsIDQuery()
                .GetScalarResult<int>(0);
            if (previouslyPublishedVersionID == 0)
            {
                return Enumerable.Empty<string>();
            }

            var previouslyPublishedNode = queueItem.Node.VersionManager.GetVersion(previouslyPublishedVersionID, queueItem.Node);
            var previouslyPublishedNodeData = algoliaObjectGenerator.GetTreeNodeData(new AlgoliaQueueItem(previouslyPublishedNode, AlgoliaTaskType.CREATE, algoliaIndex.IndexName));

            return algoliaObjectGenerator.SplitData(previouslyPublishedNodeData, algoliaIndex).Select(obj => obj.Value<string>("objectID"));
        }


        private IEnumerable<JObject> GetDataToUpsert(AlgoliaQueueItem queueItem)
        {
            var algoliaIndex = IndexStore.Instance.Get(queueItem.IndexName);
            if (algoliaIndex.DistinctOptions != null)
            {
                // If the data is split, force CREATE type to push all data to Algolia
                var nodeData = algoliaObjectGenerator.GetTreeNodeData(new AlgoliaQueueItem(queueItem.Node, AlgoliaTaskType.CREATE, queueItem.IndexName));
                return algoliaObjectGenerator.SplitData(nodeData, algoliaIndex);
            }

            return new JObject[] { algoliaObjectGenerator.GetTreeNodeData(queueItem) };
        }


        private IEnumerable<string> GetIdsToDelete(string indexName, IEnumerable<AlgoliaQueueItem> deleteTasks)
        {
            var algoliaIndex = IndexStore.Instance.Get(indexName);
            if (algoliaIndex.DistinctOptions != null)
            {
                // Data has been split, get IDs of the smaller records
                var ids = new List<string>();
                foreach (var queueItem in deleteTasks)
                {
                    var data = GetDataToUpsert(queueItem);
                    ids.AddRange(data.Select(obj => obj.Value<string>("objectID")));
                }

                return ids;
            }

            return deleteTasks.Select(queueItem => queueItem.Node.DocumentID.ToString());
        }
    }
}
