using System.Collections.Generic;

using Algolia.Search.Clients;

using CMS.Core;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.Helpers.Caching.Abstractions;
using CMS.WorkflowEngine;

using Kentico.Xperience.Algolia.Models;
using Kentico.Xperience.Algolia.Services;

using Newtonsoft.Json.Linq;

using NSubstitute;

using NUnit.Framework;

using static Kentico.Xperience.Algolia.Test.TestSearchModels;

namespace Kentico.Xperience.Algolia.Test
{
    internal class IAlgoliaClientTests
    {
        [TestFixture]
        internal class DeletetRecordsTests : AlgoliaTest
        {
            private IAlgoliaClient algoliaClient;
            private IAlgoliaObjectGenerator algoliaObjectGenerator;


            [SetUp]
            public void DeleteRecordsTestsSetUp()
            {
                var mockIndexService = Substitute.For<IAlgoliaIndexService>();
                mockIndexService.InitializeIndex(Arg.Any<string>()).Returns(args => new MockSearchIndex());

                var mockEventLogService = new MockEventLogService();

                algoliaObjectGenerator = new DefaultAlgoliaObjectGenerator(Substitute.For<IConversionService>(), mockEventLogService);
                algoliaClient = new DefaultAlgoliaClient(mockIndexService,
                    algoliaObjectGenerator,
                    Substitute.For<ICacheAccessor>(),
                    mockEventLogService,
                    Substitute.For<IVersionHistoryInfoProvider>(),
                    Substitute.For<IWorkflowStepInfoProvider>(),
                    Substitute.For<IProgressiveCache>(),
                    Substitute.For<ISearchClient>());
            }


            [Test]
            public void DeleteRecords_ValidIndex_ReturnsProcessedCount()
            {
                var enQueueItem = new AlgoliaQueueItem(FakeNodes.ArticleEn, AlgoliaTaskType.DELETE, nameof(ArticleEnSearchModel));
                var objectIdEn = algoliaObjectGenerator.GetTreeNodeData(enQueueItem).Value<string>("objectID");
                var czQueueItem = new AlgoliaQueueItem(FakeNodes.ArticleCz, AlgoliaTaskType.DELETE, nameof(ArticleEnSearchModel));
                var objectIdCz = algoliaObjectGenerator.GetTreeNodeData(czQueueItem).Value<string>("objectID");
                var numProcessed = algoliaClient.DeleteRecords(new string[] { objectIdEn, objectIdCz }, nameof(ArticleEnSearchModel));

                Assert.That(numProcessed, Is.EqualTo(2));
            }
        }


        [TestFixture]
        internal class ProcessAlgoliaTasksTests : AlgoliaTest
        {
            private IAlgoliaClient algoliaClient;


            [SetUp]
            public void ProcessAlgoliaTasksTestsSetUp()
            {
                var mockIndexService = Substitute.For<IAlgoliaIndexService>();
                mockIndexService.InitializeIndex(Arg.Any<string>()).Returns(args => new MockSearchIndex());
                algoliaClient = new DefaultAlgoliaClient(mockIndexService,
                    Substitute.For<IAlgoliaObjectGenerator>(),
                    Substitute.For<ICacheAccessor>(),
                    new MockEventLogService(),
                    Substitute.For<IVersionHistoryInfoProvider>(),
                    Substitute.For<IWorkflowStepInfoProvider>(),
                    Substitute.For<IProgressiveCache>(),
                    Substitute.For<ISearchClient>());
            }


            [Test]
            public void ProcessAlgoliaTasks_ValidTasks_ReturnsProcessedCount()
            {
                var queueItems = new List<AlgoliaQueueItem>
                {
                    new AlgoliaQueueItem(FakeNodes.ArticleEn, AlgoliaTaskType.CREATE, nameof(ArticleEnSearchModel)),
                    new AlgoliaQueueItem(FakeNodes.ArticleCz, AlgoliaTaskType.DELETE, nameof(ArticleEnSearchModel)),
                    new AlgoliaQueueItem(FakeNodes.ProductEn, AlgoliaTaskType.UPDATE, nameof(ProductsSearchModel), new string[] { "DocumentName" })
                };
                var numProcessed = algoliaClient.ProcessAlgoliaTasks(queueItems);

                Assert.That(numProcessed, Is.EqualTo(3));
            }
        }


        [TestFixture]
        internal class UpsertRecordsTests : AlgoliaTest
        {
            private IAlgoliaClient algoliaClient;
            private IAlgoliaObjectGenerator algoliaObjectGenerator;


            [SetUp]
            public void UpsertRecordsTestsSetUp()
            {
                var mockIndexService = Substitute.For<IAlgoliaIndexService>();
                mockIndexService.InitializeIndex(Arg.Any<string>()).Returns(args => new MockSearchIndex());

                var mockEventLogService = new MockEventLogService();

                algoliaObjectGenerator = new DefaultAlgoliaObjectGenerator(Substitute.For<IConversionService>(), mockEventLogService); 
                algoliaClient = new DefaultAlgoliaClient(mockIndexService,
                    algoliaObjectGenerator,
                    Substitute.For<ICacheAccessor>(),
                    mockEventLogService,
                    Substitute.For<IVersionHistoryInfoProvider>(),
                    Substitute.For<IWorkflowStepInfoProvider>(),
                    Substitute.For<IProgressiveCache>(),
                    Substitute.For<ISearchClient>());
            }


            [Test]
            public void UpsertRecords_ValidIndex_ReturnsProcessedCount()
            {
                var enQueueItem = new AlgoliaQueueItem(FakeNodes.ArticleEn, AlgoliaTaskType.CREATE, nameof(ArticleEnSearchModel));
                var dataEn = algoliaObjectGenerator.GetTreeNodeData(enQueueItem);
                var czQueueItem = new AlgoliaQueueItem(FakeNodes.ArticleCz, AlgoliaTaskType.CREATE, nameof(ArticleEnSearchModel));
                var dataCz = algoliaObjectGenerator.GetTreeNodeData(czQueueItem);
                var numProcessed = algoliaClient.UpsertRecords(new JObject[] { dataEn, dataCz }, nameof(ArticleEnSearchModel));

                Assert.That(numProcessed, Is.EqualTo(2));
            }
        }
    }
}
