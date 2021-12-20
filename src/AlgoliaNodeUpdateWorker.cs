using CMS.Base;
using CMS.DocumentEngine;

namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Thread worker which enqueues recently updated nodes indexed by Algolia and updates
    /// the Algolia indexes in the background thread.
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
        /// Adds a <see cref="TreeNode"/> to the worker queue to be indexed.
        /// </summary>
        /// <param name="updatedNode"></param>
        public static void EnqueueNodeUpdate(TreeNode updatedNode)
        {
            //TODO: Validate updatedNode
            Current.Enqueue(updatedNode, false);
        }


        protected override void Finish()
        {
        }


        protected override void ProcessItem(TreeNode item)
        {
            //TODO: Validate item
            foreach (var index in AlgoliaSearchHelper.RegisteredIndexes)
            {
                if (!AlgoliaSearchHelper.IsNodeIndexedByIndex(item, index.Key))
                {
                    continue;
                }

                var connection = new AlgoliaConnection(index.Key);
                connection.UpsertTreeNode(item);
            }
        }
    }
}