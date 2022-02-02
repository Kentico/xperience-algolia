using Algolia.Search.Clients;

using CMS.Core;
using CMS.DocumentEngine;
using CMS.Tests;

using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Services;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using System.Collections.Generic;
using System.Reflection;

using Tests.DocumentEngine;

[assembly: Category("Algolia")]
namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class AlgoliaTests : UnitTests
    {
        protected AlgoliaRegistrationService algoliaRegistrationService;
        protected AlgoliaIndexingService algoliaIndexingService;
        protected AlgoliaSearchService algoliaSearchService;
        protected IEventLogService eventLogService;

        public const string APPLICATION_ID = "my-app";
        public const string API_KEY = "my-key";


        private Dictionary<string, string> keys = new Dictionary<string, string>
        {
            {"xperience.algolia:applicationId", APPLICATION_ID},
            {"xperience.algolia:apiKey", API_KEY}
        };


        protected override void RegisterTestServices()
        {
            base.RegisterTestServices();
            
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(keys)
                .Build();
            var algoliaOptions = configuration.GetSection(AlgoliaOptions.SECTION_NAME).Get<AlgoliaOptions>();
            var searchClient = new SearchClient(new SearchConfig(algoliaOptions.ApplicationId, algoliaOptions.ApiKey), new MockHttpRequester());

            Service.Use<ISearchClient>(searchClient);
            Service.Use<IEventLogService, MockEventLogService>();
        }


        [SetUp]
        public void SetUp()
        {
            algoliaRegistrationService = Service.Resolve<AlgoliaRegistrationService>();
            algoliaIndexingService = Service.Resolve<AlgoliaIndexingService>();
            algoliaSearchService = Service.Resolve<AlgoliaSearchService>();
            eventLogService = Service.Resolve<IEventLogService>();

            // Register Algolia indexes
            var attributes = algoliaRegistrationService.GetAlgoliaIndexAttributes(Assembly.GetExecutingAssembly());
            foreach (var attribute in attributes)
            {
                algoliaRegistrationService.RegisterIndex(attribute.IndexName, attribute.Type);
            }

            // Register document types for faking
            DocumentGenerator.RegisterDocumentType<TreeNode>(FakeNodes.DOCTYPE_ARTICLE);
            DocumentGenerator.RegisterDocumentType<TreeNode>(FakeNodes.DOCTYPE_PRODUCT);
            Fake().DocumentType<TreeNode>(FakeNodes.DOCTYPE_ARTICLE);
            Fake().DocumentType<TreeNode>(FakeNodes.DOCTYPE_PRODUCT);

            // Create TreeNodes
            FakeNodes.MakeNode("/Articles/1", FakeNodes.DOCTYPE_ARTICLE);
            FakeNodes.MakeNode("/CZ/Articles/1", FakeNodes.DOCTYPE_ARTICLE, "cs-CZ");
            FakeNodes.MakeNode("/Store/Products/1", FakeNodes.DOCTYPE_PRODUCT);
            FakeNodes.MakeNode("/CZ/Store/Products/2", FakeNodes.DOCTYPE_PRODUCT, "cs-CZ");
            FakeNodes.MakeNode("/Unindexed/Product", FakeNodes.DOCTYPE_PRODUCT);
            FakeNodes.MakeNode("/Scheduled/Article", FakeNodes.DOCTYPE_ARTICLE);
        }


        [TearDown]
        public void TearDown()
        {
            FakeNodes.ClearNodes();
        }
    }
}
