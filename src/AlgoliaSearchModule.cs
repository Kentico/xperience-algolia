using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch;
using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: AssemblyDiscoverable]
[assembly: RegisterModule(typeof(AlgoliaSearchModule))]
namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Initializes the Algolia integration by scanning assemblies for custom models containing the
    /// <see cref="RegisterAlgoliaIndexAttribute"/> and stores them in <see cref="AlgoliaSearchHelper.RegisteredIndexes"/>.
    /// Also registers event handlers required for indexing content.
    /// </summary>
    public class AlgoliaSearchModule : CMS.DataEngine.Module
    {
        public AlgoliaSearchModule() : base(nameof(AlgoliaSearchModule))
        {
        }


        protected override void OnInit()
        {
            base.OnInit();
            RegisterAlgoliaIndexes();
            DocumentEvents.Update.Before += LogTreeNodeUpdate;
            DocumentEvents.Insert.After += LogTreeNodeInsert;
            DocumentEvents.Delete.After += LogTreeNodeDelete;
            RequestEvents.RunEndRequestTasks.Execute += (sender, eventArgs) => AlgoliaQueueWorker.Current.EnsureRunningThread();
        }


        private void LogTreeNodeDelete(object sender, DocumentEventArgs e)
        {
            if (EventShouldCancel(e.Node, true))
            {
                return;
            }

            EnqueueAlgoliaItems(e.Node, true, false);
        }


        private void LogTreeNodeInsert(object sender, DocumentEventArgs e)
        {
            if (EventShouldCancel(e.Node, false))
            {
                return;
            }

            EnqueueAlgoliaItems(e.Node, false, true);
        }


        private void LogTreeNodeUpdate(object sender, DocumentEventArgs e)
        {
            if (EventShouldCancel(e.Node, false))
            {
                return;
            }

            EnqueueAlgoliaItems(e.Node, false, false);
        }


        private void EnqueueAlgoliaItems(TreeNode node, bool wasDeleted, bool isNew)
        {
            foreach (var index in AlgoliaSearchHelper.RegisteredIndexes)
            {
                if (!AlgoliaSearchHelper.IsNodeIndexedByIndex(node, index.Key))
                {
                    continue;
                }

                var indexedColumns = AlgoliaSearchHelper.GetIndexedColumnNames(index.Key);
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


        private bool EventShouldCancel(TreeNode node, bool wasDeleted)
        {
            return !AlgoliaSearchHelper.IsNodeAlgoliaIndexed(node) ||
                (!wasDeleted && !node.IsPublished);
        }


        private void RegisterAlgoliaIndexes()
        {
            var attributes = new List<RegisterAlgoliaIndexAttribute>();
            var assemblies = AssemblyDiscoveryHelper.GetAssemblies(discoverableOnly: true);

            foreach (var assembly in assemblies)
            {
                attributes.AddRange(GetAlgoliaIndexAttributes(assembly));
            }

            foreach (var attribute in attributes)
            {
                AlgoliaSearchHelper.RegisterIndex(attribute.IndexName, attribute.Type);
            }
        }


        public static IEnumerable<RegisterAlgoliaIndexAttribute> GetAlgoliaIndexAttributes(Assembly assembly)
        {
            var attributes = Enumerable.Empty<RegisterAlgoliaIndexAttribute>();

            try
            {
                attributes = assembly.GetCustomAttributes(typeof(RegisterAlgoliaIndexAttribute), false)
                                    .Cast<RegisterAlgoliaIndexAttribute>();
            }
            catch (Exception exception)
            {
                var error = new DiscoveryError(exception, assembly.FullName);
                error.LogEvent();
            }

            return attributes;
        }
    }
}