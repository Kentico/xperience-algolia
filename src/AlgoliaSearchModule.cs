using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch;
using Kentico.Xperience.AlgoliaSearch.Attributes;

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
            DocumentEvents.Update.After += LogTreeNodeUpdate;
            RequestEvents.RunEndRequestTasks.Execute += (sender, eventArgs) => AlgoliaNodeUpdateWorker.Current.EnsureRunningThread();
        }


        private void LogTreeNodeUpdate(object sender, DocumentEventArgs e)
        {
            if (!e.Node.IsPublished || !AlgoliaSearchHelper.IsNodeAlgoliaIndexed(e.Node))
            {
                return;
            }

            //TODO: Check updated columns
            AlgoliaNodeUpdateWorker.EnqueueNodeUpdate(e.Node);
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