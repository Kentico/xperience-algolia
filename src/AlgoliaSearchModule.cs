using Algolia.Search.Clients;

using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DocumentEngine;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch;
using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Services;

using System;
using System.Configuration;
using System.Runtime.CompilerServices;

[assembly: AssemblyDiscoverable]
[assembly: InternalsVisibleTo("Kentico.Xperience.AlgoliaSearch.Tests")]
[assembly: RegisterModule(typeof(AlgoliaSearchModule))]
namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Initializes the Algolia integration by scanning assemblies for custom models containing the
    /// <see cref="RegisterAlgoliaIndexAttribute"/> and stores them in <see cref="IAlgoliaRegistrationService.RegisteredIndexes"/>.
    /// Also registers event handlers required for indexing content.
    /// </summary>
    public class AlgoliaSearchModule : CMS.DataEngine.Module
    {
        private IAlgoliaIndexingService algoliaIndexingService;
        private IAlgoliaRegistrationService algoliaRegistrationService;
        private IAlgoliaSearchService algoliaSearchService;


        public AlgoliaSearchModule() : base(nameof(AlgoliaSearchModule))
        {
        }


        protected override void OnPreInit()
        {
            base.OnPreInit();

            // Register ISearchClient for CMS application
            if (SystemContext.IsCMSRunningAsMainApplication)
            {
                var applicationId = ValidationHelper.GetString(ConfigurationManager.AppSettings["AlgoliaApplicationId"], String.Empty);
                var apiKey = ValidationHelper.GetString(ConfigurationManager.AppSettings["AlgoliaApiKey"], String.Empty);
                if (String.IsNullOrEmpty(applicationId) || String.IsNullOrEmpty(apiKey))
                {
                    // Algolia configuration is not valid, but IEventLogService can't be resolved during OnPreInit.
                    // Set dummy values so that DI is not broken, but errors are still logged later in the initialization
                    applicationId = "NO_APP";
                    apiKey = "NO_KEY";
                }

                var client = new SearchClient(applicationId, apiKey);
                Service.Use<ISearchClient>(client);
            }
        }


        /// <summary>
        /// Registers all Algolia indexes, initializes page event handlers, and ensures the thread
        /// queue worker for processing Algolia tasks.
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();

            algoliaIndexingService = Service.Resolve<IAlgoliaIndexingService>();
            algoliaRegistrationService = Service.Resolve<IAlgoliaRegistrationService>();
            algoliaSearchService = Service.Resolve<IAlgoliaSearchService>();
            algoliaRegistrationService.RegisterAlgoliaIndexes();

            DocumentEvents.Update.Before += LogTreeNodeUpdate;
            DocumentEvents.Insert.After += LogTreeNodeInsert;
            DocumentEvents.Delete.After += LogTreeNodeDelete;
            WorkflowEvents.Publish.After += LogTreeNodePublish;
            WorkflowEvents.Archive.After += LogTreeNodeArchive;
            RequestEvents.RunEndRequestTasks.Execute += (sender, eventArgs) => AlgoliaQueueWorker.Current.EnsureRunningThread();
        }


        /// <summary>
        /// Returns <c>true</c> if the event event handler should continue processing and log
        /// an Algolia task.
        /// </summary>
        private bool EventShouldContinue(TreeNode node, string eventName)
        {
            if (node.GetWorkflow() != null &&
                (eventName.Equals(DocumentEvents.Insert.Name, StringComparison.OrdinalIgnoreCase) || eventName.Equals(DocumentEvents.Update.Name, StringComparison.OrdinalIgnoreCase)))
            {
                // If page is under workflow, don't log tasks for update or insert events
                return false;
            }

            return algoliaSearchService.IsIndexingEnabled() &&
                algoliaRegistrationService.IsNodeAlgoliaIndexed(node);
        }


        /// <summary>
        /// Called after a page is archived. Logs an Algolia task to be processed later.
        /// </summary>
        private void LogTreeNodeArchive(object sender, WorkflowEventArgs e)
        {
            if (!EventShouldContinue(e.Document, WorkflowEvents.Archive.Name))
            {
                return;
            }

            algoliaIndexingService.EnqueueAlgoliaItems(e.Document, WorkflowEvents.Archive.Name);
        }


        /// <summary>
        /// Called after a page is published manually or by content scheduling. Logs an Algolia
        /// task to be processed later.
        /// </summary>
        private void LogTreeNodePublish(object sender, WorkflowEventArgs e)
        {
            if (!EventShouldContinue(e.Document, WorkflowEvents.Publish.Name))
            {
                return;
            }

            algoliaIndexingService.EnqueueAlgoliaItems(e.Document, WorkflowEvents.Publish.Name);
        }


        /// <summary>
        /// Called after a page is deleted. Logs an Algolia task to be processed later.
        /// </summary>
        private void LogTreeNodeDelete(object sender, DocumentEventArgs e)
        {
            if (!EventShouldContinue(e.Node, DocumentEvents.Delete.Name))
            {
                return;
            }

            algoliaIndexingService.EnqueueAlgoliaItems(e.Node, DocumentEvents.Delete.Name);
        }


        /// <summary>
        /// Called after a page is created. Logs an Algolia task to be processed later.
        /// </summary>
        private void LogTreeNodeInsert(object sender, DocumentEventArgs e)
        {
            if (!EventShouldContinue(e.Node, DocumentEvents.Insert.Name))
            {
                return;
            }

            algoliaIndexingService.EnqueueAlgoliaItems(e.Node, DocumentEvents.Insert.Name);
        }


        /// <summary>
        /// Called before a page is updated. Logs an Algolia task to be processed later.
        /// </summary>
        private void LogTreeNodeUpdate(object sender, DocumentEventArgs e)
        {
            if (!EventShouldContinue(e.Node, DocumentEvents.Update.Name))
            {
                return;
            }

            algoliaIndexingService.EnqueueAlgoliaItems(e.Node, DocumentEvents.Update.Name);
        }
    }
}