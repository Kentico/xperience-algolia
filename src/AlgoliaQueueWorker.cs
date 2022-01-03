using CMS.Base;
using CMS.Core;

using Kentico.Xperience.AlgoliaSearch.Models;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Thread worker which enqueues recently created, updated, or deleted nodes
    /// indexed by Algolia and updates the Algolia indexes in the background thread.
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
        /// <param name="updatedNode"></param>
        public static void EnqueueAlgoliaQueueItem(AlgoliaQueueItem queueItem)
        {
            if (queueItem == null || queueItem.Node == null || String.IsNullOrEmpty(queueItem.IndexName))
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
        /// Processes multiple queue items from all Algolia indexes in batches. Algolia
        /// automatically applies batching in multiples of 1,000 when using their API,
        /// so all queue items are forwarded to the API.
        /// </summary>
        /// <param name="items">The items to process.</param>
        /// <returns>The number of processed items.</returns>
        protected override int ProcessItems(IEnumerable<AlgoliaQueueItem> items)
        {
            // Group queue items based on index name
            var groups = items.ToList().GroupBy(item => item.IndexName);
            foreach (var group in groups)
            {
                try
                {
                    var connection = new AlgoliaConnection(group.Key);
                    var deleteTasks = group.Where(queueItem => queueItem.Deleted);
                    var updateTasks = group.Where(queueItem => !queueItem.Deleted);

                    connection.UpsertTreeNodes(updateTasks.Select(queueItem => queueItem.Node));
                    connection.DeleteTreeNodes(deleteTasks.Select(queueItem => queueItem.Node));
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


        private void LogError(string message, string code)
        {
            mEventLogService.LogError(nameof(AlgoliaQueueWorker), code, message);
        }
    }
}