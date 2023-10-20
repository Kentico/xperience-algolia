using System;
using System.Linq;

using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.Algolia.Extensions;
using Kentico.Xperience.Algolia.Models;

namespace Kentico.Xperience.Algolia.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaTaskLogger"/>.
    /// </summary>
    internal class DefaultAlgoliaTaskLogger : IAlgoliaTaskLogger
    {
        private readonly IEventLogService eventLogService;

        public DefaultAlgoliaTaskLogger(IEventLogService eventLogService) {
            this.eventLogService = eventLogService;
        }


        /// <inheritdoc />
        public void HandleEvent(TreeNode node, string eventName)
        {
            if ((eventName.Equals(DocumentEvents.Insert.Name, StringComparison.OrdinalIgnoreCase) || eventName.Equals(DocumentEvents.Update.Name, StringComparison.OrdinalIgnoreCase))
                && node.GetWorkflow() != null)
            {
                // Do not handle Insert/Update for pages under workflow
                return;
            }

            foreach (var indexName in IndexStore.Instance.GetAll().Select(index => index.IndexName))
            {
                if (!node.IsIndexedByIndex(indexName))
                {
                    continue;
                }

                try
                {
                    var queueItem = new AlgoliaQueueItem(node, GetTaskType(node, eventName), indexName, node.ChangedColumns());
                    AlgoliaQueueWorker.Current.EnqueueAlgoliaQueueItem(queueItem);
                }
                catch (InvalidOperationException ex)
                {
                    eventLogService.LogException(nameof(DefaultAlgoliaTaskLogger), nameof(HandleEvent), ex);
                }
            }
        }

        public void HandleEventAfter(TreeNode node, string eventName)
        {
            var treeProvider = new TreeProvider();
            if (eventName != DocumentEvents.Delete.Name)
            {
                node = treeProvider.SelectSingleDocument(node.DocumentID, coupledData: true);
            }
            
            foreach (var indexName in IndexStore.Instance.GetAll().Select(index => index.IndexName))
            {
                if (!node.IsIndexedByIndex(indexName))
                {
                    continue;
                }

                try
                {
                    var queueItem = new AlgoliaQueueItem(node, GetTaskType(node, eventName), indexName, node.ChangedColumns());
                    AlgoliaQueueWorker.Current.EnqueueAlgoliaQueueItem(queueItem);
                }
                catch (InvalidOperationException ex)
                {
                    eventLogService.LogException(nameof(DefaultAlgoliaTaskLogger), nameof(HandleEvent), ex);
                }
            }
        }

        private AlgoliaTaskType GetTaskType(TreeNode node, string eventName)
        {
            if (eventName.Equals(DocumentEvents.Insert.Name, StringComparison.OrdinalIgnoreCase) ||
                (eventName.Equals(WorkflowEvents.Publish.Name, StringComparison.OrdinalIgnoreCase) && (node.WorkflowHistory == null || node.WorkflowHistory.Count == 0)))
            {
                return AlgoliaTaskType.CREATE;
            }

            if (eventName.Equals(DocumentEvents.Update.Name, StringComparison.OrdinalIgnoreCase) ||
                (eventName.Equals(WorkflowEvents.Publish.Name, StringComparison.OrdinalIgnoreCase) && node.WorkflowHistory.Count > 0))
            {
                return AlgoliaTaskType.UPDATE;
            }

            if (eventName.Equals(DocumentEvents.Delete.Name, StringComparison.OrdinalIgnoreCase) ||
                eventName.Equals(WorkflowEvents.Archive.Name, StringComparison.OrdinalIgnoreCase))
            {
                return AlgoliaTaskType.DELETE;
            }

            return AlgoliaTaskType.UNKNOWN;
        }
    }
}
