using CMS.Base;
using CMS.DocumentEngine;

namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Thread worker which enqueues recently created, updated, or deleted nodes
    /// indexed by Algolia and updates the Algolia indexes in the background thread.
    /// </summary>
    public class AlgoliaNodeUpdateWorker : ThreadQueueWorker<TreeNode, AlgoliaNodeUpdateWorker>
    {
        protected override int DefaultInterval => 60000;


        /// <summary>
        /// Constructor.
        /// </summary>
        public AlgoliaNodeUpdateWorker()
        {
        }


        /// <summary>
        /// Adds a <see cref="TreeNode"/> to the worker queue to be processed.
        /// </summary>
        /// <param name="updatedNode"></param>
        public static void EnqueueNodeUpdate(TreeNode updatedNode)
        {
            if (updatedNode == null)
            {
                return;
            }

            Current.Enqueue(updatedNode, false);
        }


        protected override void Finish()
        {
        }


        protected override void ProcessItem(TreeNode item)
        {
            // Determine if node was deleted
            // TODO: Consider removing DB query and use a flag to denote deleted nodes
            var existingNode = new TreeProvider().SelectSingleNode(item.NodeID, item.DocumentCulture);
            var doDelete = (existingNode == null);

            foreach (var index in AlgoliaSearchHelper.RegisteredIndexes)
            {
                if (!AlgoliaSearchHelper.IsNodeIndexedByIndex(item, index.Key))
                {
                    continue;
                }

                var connection = new AlgoliaConnection(index.Key);
                if (doDelete)
                {
                    connection.DeleteTreeNode(item);
                }
                else
                {
                    connection.UpsertTreeNode(item);
                }
            }
        }
    }
}