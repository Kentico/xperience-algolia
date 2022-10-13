using System;
using System.Collections.Generic;
using System.Linq;

using Algolia.Search.Clients;
using Algolia.Search.Models.Common;

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

using static Kentico.Xperience.Algolia.Tests.TestSearchModels;

namespace Kentico.Xperience.Algolia.Tests
{
    internal class DefaultAlgoliaClientTests
    {
        private static ISearchIndex GetMockSearchIndex()
        {
            var mockSearchIndex = Substitute.For<ISearchIndex>();
            mockSearchIndex.DeleteObjects(Arg.Any<IEnumerable<string>>(), null)
                .ReturnsForAnyArgs(args => new BatchIndexingResponse
                {
                    Responses = new List<BatchResponse>
                    {
                        new BatchResponse
                        {
                            ObjectIDs = args.Arg<IEnumerable<string>>()
                        }
                    }
                }
            );
            mockSearchIndex.PartialUpdateObjects(Arg.Any<IEnumerable<JObject>>(), null, Arg.Any<bool>())
                .ReturnsForAnyArgs(args => new BatchIndexingResponse
                {
                    Responses = new List<BatchResponse>
                    {
                        new BatchResponse
                        {
                            ObjectIDs = new string[args.Arg<IEnumerable<JObject>>().Count()]
                        }
                    }
                }
            );

            return mockSearchIndex;
        }


        [TestFixture]
        internal class DeletetRecordsTests : AlgoliaTests
        {
            private IAlgoliaClient algoliaClient;
            private IAlgoliaObjectGenerator algoliaObjectGenerator;
            private readonly ISearchIndex mockSearchIndex = GetMockSearchIndex();


            [SetUp]
            public void DeleteRecordsTestsSetUp()
            {
                var mockIndexService = Substitute.For<IAlgoliaIndexService>();
                mockIndexService.InitializeIndex(Arg.Any<string>()).Returns(args => mockSearchIndex);

                algoliaObjectGenerator = new DefaultAlgoliaObjectGenerator(Substitute.For<IConversionService>(), Substitute.For<IEventLogService>());
                algoliaClient = new DefaultAlgoliaClient(mockIndexService,
                    algoliaObjectGenerator,
                    Substitute.For<ICacheAccessor>(),
                    Substitute.For<IEventLogService>(),
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
                var objectIds = new string[] { objectIdEn, objectIdCz };
                var numProcessed = algoliaClient.DeleteRecords(objectIds, nameof(ArticleEnSearchModel));

                Assert.That(numProcessed, Is.EqualTo(2));
                mockSearchIndex.Received(1).DeleteObjects(Arg.Is<IEnumerable<string>>(arg => arg.SequenceEqual(objectIds)), null);
            }
        }


        [TestFixture]
        internal class ProcessAlgoliaTasksTests : AlgoliaTests
        {
            private IAlgoliaClient algoliaClient;
            private IAlgoliaObjectGenerator algoliaObjectGenerator;
            private readonly ISearchIndex mockSearchIndex = GetMockSearchIndex();


            [SetUp]
            public void ProcessAlgoliaTasksTestsSetUp()
            {
                var mockIndexService = Substitute.For<IAlgoliaIndexService>();
                mockIndexService.InitializeIndex(Arg.Any<string>()).ReturnsForAnyArgs(mockSearchIndex);
                algoliaObjectGenerator = new DefaultAlgoliaObjectGenerator(Substitute.For<IConversionService>(), Substitute.For<IEventLogService>());
                algoliaClient = new DefaultAlgoliaClient(mockIndexService,
                    algoliaObjectGenerator,
                    Substitute.For<ICacheAccessor>(),
                    Substitute.For<IEventLogService>(),
                    Substitute.For<IVersionHistoryInfoProvider>(),
                    Substitute.For<IWorkflowStepInfoProvider>(),
                    Substitute.For<IProgressiveCache>(),
                    Substitute.For<ISearchClient>());
            }


            [Test]
            public void ProcessAlgoliaTasks_ValidTasks_ReturnsProcessedCount()
            {
                var createQueueItem = new AlgoliaQueueItem(FakeNodes.ArticleEn, AlgoliaTaskType.CREATE, nameof(ArticleEnSearchModel));
                var deleteQueueItem = new AlgoliaQueueItem(FakeNodes.ArticleCz, AlgoliaTaskType.DELETE, nameof(ArticleEnSearchModel));
                var updateQueueItem = new AlgoliaQueueItem(FakeNodes.ArticleEn, AlgoliaTaskType.UPDATE, nameof(ArticleEnSearchModel), new string[] { "DocumentName" });
                var IdToDelete = algoliaObjectGenerator.GetTreeNodeData(deleteQueueItem).Value<string>("objectID");
                var dataToUpsert = new JObject[] {
                    algoliaObjectGenerator.GetTreeNodeData(createQueueItem),
                    algoliaObjectGenerator.GetTreeNodeData(updateQueueItem)
                };
                var numProcessed = algoliaClient.ProcessAlgoliaTasks(new AlgoliaQueueItem[] { createQueueItem, updateQueueItem, deleteQueueItem });

                Assert.That(numProcessed, Is.EqualTo(3));
                mockSearchIndex.Received(1).DeleteObjects(Arg.Is<IEnumerable<string>>(arg => arg.SequenceEqual(new string[] { IdToDelete })), null);
                mockSearchIndex.Received(1).PartialUpdateObjects(
                    Arg.Is<IEnumerable<JObject>>(arg => arg.SequenceEqual(dataToUpsert, new JObjectEqualityComparer())), null, true);
            }
        }


        [TestFixture]
        internal class GetStatisticsTests : AlgoliaTests
        {
            private IAlgoliaClient algoliaClient;
            private readonly IProgressiveCache mockProgressiveCache = Substitute.For<IProgressiveCache>();
            private readonly ISearchClient mockSearchClient = Substitute.For<ISearchClient>();


            [SetUp]
            public void GetStatisticsTestsSetUp()
            {
                mockSearchClient.ListIndices(null).ReturnsForAnyArgs(args =>
                    new ListIndicesResponse
                    {
                        Items = new List<IndicesResponse>()
                    }
                );
                mockProgressiveCache.Load(Arg.Any<Func<CacheSettings, List<IndicesResponse>>>(), Arg.Any<CacheSettings>()).ReturnsForAnyArgs(args =>
                {
                    // Execute the passed function
                    args.ArgAt<Func<CacheSettings, List<IndicesResponse>>>(0)(args.ArgAt<CacheSettings>(1));

                    return null;
                });

                algoliaClient = new DefaultAlgoliaClient(Substitute.For<IAlgoliaIndexService>(),
                    Substitute.For<IAlgoliaObjectGenerator>(),
                    Substitute.For<ICacheAccessor>(),
                    Substitute.For<IEventLogService>(),
                    Substitute.For<IVersionHistoryInfoProvider>(),
                    Substitute.For<IWorkflowStepInfoProvider>(),
                    mockProgressiveCache,
                    mockSearchClient);
            }


            [Test]
            public void GetStatistics_CallsMethods()
            {
                algoliaClient.GetStatistics();
                mockSearchClient.Received(1).ListIndices(null);
                mockProgressiveCache.Received(1).Load(Arg.Any<Func<CacheSettings, List<IndicesResponse>>>(), Arg.Any<CacheSettings>());
            }
        }


        [TestFixture]
        internal class UpsertRecordsTests : AlgoliaTests
        {
            private IAlgoliaClient algoliaClient;
            private IAlgoliaObjectGenerator algoliaObjectGenerator;
            private readonly ISearchIndex mockSearchIndex = GetMockSearchIndex();


            [SetUp]
            public void UpsertRecordsTestsSetUp()
            {
                var mockIndexService = Substitute.For<IAlgoliaIndexService>();
                mockIndexService.InitializeIndex(Arg.Any<string>()).Returns(args => mockSearchIndex);

                algoliaObjectGenerator = new DefaultAlgoliaObjectGenerator(Substitute.For<IConversionService>(), Substitute.For<IEventLogService>()); 
                algoliaClient = new DefaultAlgoliaClient(mockIndexService,
                    algoliaObjectGenerator,
                    Substitute.For<ICacheAccessor>(),
                    Substitute.For<IEventLogService>(),
                    Substitute.For<IVersionHistoryInfoProvider>(),
                    Substitute.For<IWorkflowStepInfoProvider>(),
                    Substitute.For<IProgressiveCache>(),
                    Substitute.For<ISearchClient>());
            }


            [Test]
            public void UpsertRecords_ValidIndex_ReturnsProcessedCount()
            {
                var enQueueItem = new AlgoliaQueueItem(FakeNodes.ArticleEn, AlgoliaTaskType.CREATE, nameof(ArticleEnSearchModel));
                var czQueueItem = new AlgoliaQueueItem(FakeNodes.ArticleCz, AlgoliaTaskType.CREATE, nameof(ArticleEnSearchModel));
                var dataToUpsert = new JObject[] {
                    algoliaObjectGenerator.GetTreeNodeData(enQueueItem),
                    algoliaObjectGenerator.GetTreeNodeData(czQueueItem)
                };
                var numProcessed = algoliaClient.UpsertRecords(dataToUpsert, nameof(ArticleEnSearchModel));

                Assert.That(numProcessed, Is.EqualTo(2));
                mockSearchIndex.Received(1).PartialUpdateObjects(
                    Arg.Is<IEnumerable<JObject>>(arg => arg.SequenceEqual(dataToUpsert, new JObjectEqualityComparer())), null, true);
            }
        }
    }
}
