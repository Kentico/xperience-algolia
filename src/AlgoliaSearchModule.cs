using CMS;
using CMS.Base;
using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch;
using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Helpers;
using Kentico.Xperience.AlgoliaSearch.Models;

[assembly: AssemblyDiscoverable]
[assembly: RegisterModule(typeof(AlgoliaSearchModule))]
namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Initializes the Algolia integration by scanning assemblies for custom models containing the
    /// <see cref="RegisterAlgoliaIndexAttribute"/> and stores them in <see cref="AlgoliaRegistrationHelper.RegisteredIndexes"/>.
    /// Also registers event handlers required for indexing content.
    /// </summary>
    public class AlgoliaSearchModule : CMS.DataEngine.Module
    {
        public AlgoliaSearchModule() : base(nameof(AlgoliaSearchModule))
        {
        }


        /// <summary>
        /// Registers all Algolia indexes, initializes page event handlers, and ensures the thread
        /// queue worker for processing Algolia tasks.
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();
            AlgoliaRegistrationHelper.RegisterAlgoliaIndexes();
            DocumentEvents.Update.Before += LogTreeNodeUpdate;
            DocumentEvents.Insert.After += LogTreeNodeInsert;
            DocumentEvents.Delete.After += LogTreeNodeDelete;
            RequestEvents.RunEndRequestTasks.Execute += (sender, eventArgs) => AlgoliaQueueWorker.Current.EnsureRunningThread();
        }


        /// <summary>
        /// Called after a page is deleted. Logs an Algolia task to be processed later.
        /// </summary>
        private void LogTreeNodeDelete(object sender, DocumentEventArgs e)
        {
            if (EventShouldCancel(e.Node, true))
            {
                return;
            }

            EnqueueAlgoliaItems(e.Node, true, false);
        }


        /// <summary>
        /// Called after a page is created. Logs an Algolia task to be processed later.
        /// </summary>
        private void LogTreeNodeInsert(object sender, DocumentEventArgs e)
        {
            if (EventShouldCancel(e.Node, false))
            {
                return;
            }

            EnqueueAlgoliaItems(e.Node, false, true);
        }


        /// <summary>
        /// Called before a page is updated. Logs an Algolia task to be processed later.
        /// </summary>
        private void LogTreeNodeUpdate(object sender, DocumentEventArgs e)
        {
            if (EventShouldCancel(e.Node, false))
            {
                return;
            }

            EnqueueAlgoliaItems(e.Node, false, false);
        }


        /// <summary>
        /// Loops through all registered Algolia indexes and logs a task if the passed
        /// <paramref name="node"/> is indexed. For updated pages, a task is only logged
        /// if one of the indexed columns has been modified.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> that triggered the event.</param>
        /// <param name="wasDeleted">True if the <paramref name="node"/> was deleted.</param>
        /// <param name="isNew">True if the <paramref name="node"/> was created.</param>
        private void EnqueueAlgoliaItems(TreeNode node, bool wasDeleted, bool isNew)
        {
            foreach (var index in AlgoliaRegistrationHelper.RegisteredIndexes)
            {
                if (!AlgoliaRegistrationHelper.IsNodeIndexedByIndex(node, index.Key))
                {
                    continue;
                }

                var indexedColumns = AlgoliaRegistrationHelper.GetIndexedColumnNames(index.Key);
                if (!isNew && !wasDeleted && !node.AnyItemChanged(indexedColumns))
                {
                    continue;
                }

                AlgoliaQueueWorker.EnqueueAlgoliaQueueItem(new AlgoliaQueueItem()
                {
                    Node = node,
                    Deleted = wasDeleted,
                    IndexName = index.Key
                });
            }
        }


        /// <summary>
        /// Returns true if the page event event handler should stop processing. Checks
        /// if the page is indexed by any Algolia index, and for new/updated pages, the
        /// page must be published.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> that triggered the event.</param>
        /// <param name="wasDeleted">True if the <paramref name="node"/> was deleted.</param>
        /// <returns></returns>
        private bool EventShouldCancel(TreeNode node, bool wasDeleted)
        {
            return !AlgoliaRegistrationHelper.IsNodeAlgoliaIndexed(node) ||
                (!wasDeleted && !node.PublishedVersionExists);
        }
    }
}