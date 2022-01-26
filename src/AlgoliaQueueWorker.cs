using CMS.Base;
using CMS.Core;

using Kentico.Xperience.AlgoliaSearch.Helpers;
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
        /// Initializes a new instance of the <see cref="AlgoliaQueueItem"/> class. Should
        /// not be called directly- the worker should be initialized during startup using
        /// <see cref="ThreadWorker{T}.EnsureRunningThread"/>.
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


        protected override int ProcessItems(IEnumerable<AlgoliaQueueItem> items)
        {
            AlgoliaIndexingHelper.ProcessAlgoliaTasks(items);
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