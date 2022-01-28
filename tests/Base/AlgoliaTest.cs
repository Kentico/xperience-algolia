using Algolia.Search.Http;

using CMS.Core;
using CMS.DocumentEngine;
using CMS.Tests;

using Kentico.Xperience.AlgoliaSearch.Helpers;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using System.Collections.Generic;
using System.Reflection;

using Tests.DocumentEngine;

[assembly: Category("Algolia")]
namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class AlgoliaTest : UnitTests
    {
        public const string APPLICATION_ID = "my-app";
        public const string API_KEY = "my-key";

        
        protected MockEventLogService mEventLogService;


        private Dictionary<string, string> algoliaOptions = new Dictionary<string, string>
        {
            {"xperience.algolia:applicationId", APPLICATION_ID},
            {"xperience.algolia:apiKey", API_KEY}
        };


        protected override void RegisterTestServices()
        {
            base.RegisterTestServices();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(algoliaOptions)
                .Build();

            mEventLogService = new MockEventLogService();

            Service.Use<IConfiguration>(configuration);
            Service.Use<IEventLogService>(mEventLogService);
            Service.Use<IHttpRequester, MockHttpRequester>();
        }


        [SetUp]
        public void SetUp()
        {
            // Register Algolia indexes
            var attributes = AlgoliaRegistrationHelper.GetAlgoliaIndexAttributes(Assembly.GetExecutingAssembly());
            foreach (var attribute in attributes)
            {
                AlgoliaRegistrationHelper.RegisterIndex(attribute.IndexName, attribute.Type);
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
            AlgoliaRegistrationHelper.RegisteredIndexes.Clear();
            FakeNodes.ClearNodes();
        }
    }
}
