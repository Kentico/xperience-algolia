using CMS.Base;
using CMS.Core;

using Kentico.Xperience.AlgoliaSearch.Models;

using System;

namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Thread worker which enqueues recently created, updated, or deleted nodes
    /// indexed by Algolia and updates the Algolia indexes in the background thread.
    /// </summary>
    public class AlgoliaQueueWorker : ThreadQueueWorker<AlgoliaQueueItem, AlgoliaQueueWorker>
    {
        protected override int DefaultInterval => 10000;


        /// <summary>
        /// Constructor.
        /// </summary>
        public AlgoliaQueueWorker()
        {
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
        }


        protected override void ProcessItem(AlgoliaQueueItem queueItem)
        {
            try
            {
                var connection = new AlgoliaConnection(queueItem.IndexName);
                if (queueItem.Deleted)
                {
                    connection.DeleteTreeNode(queueItem.Node);
                    return;
                }

                connection.UpsertTreeNode(queueItem.Node);
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex.Message, nameof(ProcessItem));
            }
            catch (ArgumentNullException ex)
            {
                LogError(ex.Message, nameof(ProcessItem));
            }
        }


        private void LogError(string message, string code)
        {
            Service.Resolve<IEventLogService>().LogError(nameof(AlgoliaQueueWorker), code, message);
        }
    }
}