using CMS.Base;
using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Helpers;
using Kentico.Xperience.AlgoliaSearch.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Thread worker which enqueues recently created, updated, or deleted nodes,
    /// checks whether the nodes are indexed by any Algolia index, and updates the
    /// Algolia indexes in the background thread.
    /// </summary>
    public class AlgoliaQueueWorker : ThreadQueueWorker<AlgoliaQueueItem, AlgoliaQueueWorker>
    {
        private readonly IEventLogService mEventLogService;


        protected override int DefaultInterval => 10000;


        /// <summary>
        /// Constructor.
        /// </summary>
        public AlgoliaQueueWorker()
        {
            mEventLogService = Service.Resolve<IEventLogService>();
        }


        /// <summary>
        /// Adds an <see cref="AlgoliaQueueItem"/> to the worker queue to be processed.
        /// </summary>
        public static void EnqueueAlgoliaQueueItem(AlgoliaQueueItem queueItem)
        {
            if (queueItem == null || queueItem.Node == null)
            {
                return;
            }

            Current.Enqueue(queueItem, false);
        }


        protected override void Finish()
        {
            RunProcess();
        }


        /// <summary>
        /// Processes queued node changes and upserts or deletes records in Algolia.
        /// Algolia automatically applies batching in multiples of 1,000 when using
        /// their API, so all updates are forwarded to the API.
        /// </summary>
        /// <param name="items">The items to process.</param>
        /// <returns>The number of processed items.</returns>
        protected override int ProcessItems(IEnumerable<AlgoliaQueueItem> items)
        {
            var listCopy = items.ToList();
            foreach (var index in AlgoliaSearchHelper.RegisteredIndexes)
            {
                var nodesToDelete = listCopy.Where(item => item.Deleted && IsNodeIndexedByIndex(item.Node, index.Key)).ToList();

                // Add new nodes first
                var nodesToUpsert = listCopy.Where(item => item.IsNew && item.Node.PublishedVersionExists && IsNodeIndexedByIndex(item.Node, index.Key)).ToList();

                // For all update tasks, check if the node should be added or removed from Algolia
                var updateTasks = listCopy.Where(item => !item.IsNew && !item.Deleted).ToList();
                foreach (var item in updateTasks)
                {
                    if (item.Node.PublishedVersionExists && IsNodeIndexedByIndex(item.Node, index.Key))
                    {
                        nodesToUpsert.Add(item);
                    }
                    else
                    {
                        // Node shouldn't exist in this index, ensure it is removed from Algolia
                        nodesToDelete.Add(item);
                    }
                }

                // Process tasks
                try
                {
                    var connection = new AlgoliaConnection(index.Key);
                    connection.UpsertTreeNodes(nodesToUpsert.Select(queueItem => queueItem.Node));
                    connection.DeleteTreeNodes(nodesToDelete.Select(queueItem => queueItem.Node));
                }
                catch (InvalidOperationException ex)
                {
                    LogError(ex.Message, nameof(ProcessItems));
                }
                catch (ArgumentNullException ex)
                {
                    LogError(ex.Message, nameof(ProcessItems));
                }
            }

            return items.Count();
        }


        protected override void ProcessItem(AlgoliaQueueItem item)
        {
            ProcessItems(new AlgoliaQueueItem[] { item });
        }


        /// <summary>
        /// Returns true if the <paramref name="node"/> should be indexed based on the settings of
        /// the index's <see cref="IncludedPathAttribute"/>s.
        /// </summary>
        /// <param name="node">The node to check for indexing.</param>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        private bool IsNodeIndexedByIndex(TreeNode node, string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var searchModelType = AlgoliaSearchHelper.GetModelByIndexName(indexName);
            if (searchModelType == null)
            {
                LogError($"Error loading search model class for index '{indexName}.'", nameof(IsNodeIndexedByIndex));
                return false;
            }

            var includedPathAttributes = searchModelType.GetCustomAttributes<IncludedPathAttribute>(false);
            foreach (var includedPathAttribute in includedPathAttributes)
            {
                if (IsNodeIndexedByAttribute(includedPathAttribute, node))
                {
                    return true;
                }
            }

            return false;
        }


        private bool IsNodeIndexedByAttribute(IncludedPathAttribute includedPathAttribute, TreeNode node)
        {
            // Check if page is on correct path
            var path = includedPathAttribute.AliasPath;
            if (path.EndsWith("/%"))
            {
                path = path.TrimEnd('%', '/');
                if (!node.NodeAliasPath.StartsWith(path))
                {
                    return false;
                }
            }
            else
            {
                if (node.NodeAliasPath != path)
                {
                    return false;
                }
            }

            // Check page is of correct type and culture
            var matchesPageType = (includedPathAttribute.PageTypes.Length == 0 || includedPathAttribute.PageTypes.Contains(node.ClassName));
            var matchesCulture = (includedPathAttribute.Cultures.Length == 0 || includedPathAttribute.Cultures.Contains(node.DocumentCulture));
            if (!matchesPageType || !matchesCulture)
            {
                return false;
            }

            // Check if page ACL contains read permission for all index roles
            if (!AlgoliaSearchHelper.NodeMeetsPermissionRequirements(includedPathAttribute, node))
            {
                return false;
            }

            return true;
        }


        private void LogError(string message, string code)
        {
            mEventLogService.LogError(nameof(AlgoliaQueueWorker), code, message);
        }
    }
}