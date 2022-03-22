using CMS.DataEngine;
using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Services;

using Newtonsoft.Json.Linq;

using NSubstitute;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class IAlgoliaIndexingServiceTests
    {
        [TestFixture]
        internal class GetTreeNodeDataTests : AlgoliaTests
        {
            private IAlgoliaIndexingService algoliaIndexingService;


            [SetUp]
            public void GetTreeNodeDataTests_SetUp()
            {
                algoliaIndexingService = new DefaultAlgoliaIndexingService(Substitute.For<IAlgoliaRegistrationService>(), Substitute.For<IAlgoliaConnection>(), new MockEventLogService());
            }


            [Test]
            public void GetTreeNodeData_UnscheduledNode_ContainsMinMaxPublishingDates()
            {
                var unscheduledNode = FakeNodes.GetNode("/Articles/1");
                var nodeData = algoliaIndexingService.GetTreeNodeData(unscheduledNode, typeof(Model1));

                Assert.Multiple(() => {
                    Assert.AreEqual(nodeData.Value<int>(nameof(AlgoliaSearchModel.DocumentPublishFrom)), 0);
                    Assert.AreEqual(nodeData.Value<int>(nameof(AlgoliaSearchModel.DocumentPublishTo)), Int32.MaxValue);
                });
            }


            [Test]
            public void GetTreeNodeData_ScheduledNode_ContainsUnixPublishingDates()
            {
                var scheduledNode = TreeNode.New(FakeNodes.DOCTYPE_ARTICLE).With(p =>
                {
                    p.SetValue(nameof(AlgoliaSearchModel.DocumentPublishFrom), new DateTime(2022, 1, 1));
                    p.SetValue(nameof(AlgoliaSearchModel.DocumentPublishTo), new DateTime(2023, 1, 1));
                });

                var nodeData = algoliaIndexingService.GetTreeNodeData(scheduledNode, typeof(Model1));

                Assert.Multiple(() => {
                    Assert.AreEqual(nodeData.Value<int>(nameof(AlgoliaSearchModel.DocumentPublishFrom)), scheduledNode.DocumentPublishFrom.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                    Assert.AreEqual(nodeData.Value<int>(nameof(AlgoliaSearchModel.DocumentPublishTo)), scheduledNode.DocumentPublishTo.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                });
            }


            [Test]
            public void GetTreeNodeData_PropertiesFromModel_MatchNodeValue()
            {
                var node = FakeNodes.GetNode("/Articles/1");
                var nodeData = algoliaIndexingService.GetTreeNodeData(node, typeof(Model1));

                Assert.AreEqual(nodeData.Value<DateTime>(nameof(Model1.DocumentCreatedWhen)), new DateTime(2022, 1, 1));
            }


            [Test]
            public void GetTreeNodeData_PropertyWithSource_MatchNodeValue()
            {
                var nodeAliasPath = "/Articles/1";
                var node = FakeNodes.GetNode(nodeAliasPath);
                var nodeData = algoliaIndexingService.GetTreeNodeData(node, typeof(Model4));

                Assert.AreEqual(nodeData.Value<string>(nameof(Model4.Prop1)), nodeAliasPath);
            }


            [Test]
            public void GetTreeNodeData_InheritsBaseClass_ContainsNodeValuesFromAllClasses()
            {
                var node = FakeNodes.GetNode("/Articles/1");
                var data = algoliaIndexingService.GetTreeNodeData(node, typeof(Model7));

                Assert.Multiple(() => {
                    Assert.IsNotNull(data.Value<string>(nameof(Model7.NodeAliasPath)));
                    Assert.IsNotNull(data.Value<string>(nameof(ModelBaseClass.DocumentName)));
                });
            }


            [Test]
            public void GetTreeNodeData_OnIndexingPropertyHandler_ValueIsUpperCase()
            {
                var nodeAliasPath = "/Articles/1";
                var node = FakeNodes.GetNode(nodeAliasPath);
                var data = algoliaIndexingService.GetTreeNodeData(node, typeof(Model7));

                Assert.Multiple(() => {
                    Assert.AreEqual(nodeAliasPath.ToUpper(), data.Value<string>(nameof(Model7.NodeAliasPath)));
                    Assert.AreEqual("NAME", data.Value<string>(nameof(ModelBaseClass.DocumentName)));
                });
            }
        }


        [TestFixture]
        internal class ProcessAlgoliaTasksTests : AlgoliaTests
        {
            private IAlgoliaIndexingService algoliaIndexingService;


            [SetUp]
            public void ProcessAlgoliaTasksTests_SetUp()
            {
                var mockRegistrationService = Substitute.For<IAlgoliaRegistrationService>();
                mockRegistrationService.RegisteredIndexes.Returns(new List<RegisterAlgoliaIndexAttribute>() {
                    new RegisterAlgoliaIndexAttribute(typeof(Model1), Model1.IndexName),
                    new RegisterAlgoliaIndexAttribute(typeof(Model2), Model2.IndexName),
                    new RegisterAlgoliaIndexAttribute(typeof(Model3), Model3.IndexName),
                    new RegisterAlgoliaIndexAttribute(typeof(Model4), Model4.IndexName)
                });

                var mockAlgoliaConnection = Substitute.For<IAlgoliaConnection>();
                mockAlgoliaConnection.UpsertRecords(Arg.Any<IEnumerable<JObject>>()).Returns(args => args.Arg<IEnumerable<JObject>>().Count());
                mockAlgoliaConnection.DeleteRecords(Arg.Any<IEnumerable<string>>()).Returns(args => args.Arg<IEnumerable<string>>().Count());

                algoliaIndexingService = new DefaultAlgoliaIndexingService(mockRegistrationService, mockAlgoliaConnection, new MockEventLogService());
            }


            [Test]
            public void ProcessAlgoliaTasks_ListOfValidQueueItems_ReturnsProcessedCount()
            {
                var node1 = FakeNodes.GetNode("/Articles/1");
                var node2 = FakeNodes.GetNode("/CZ/Articles/1");
                var testQueueItems = new List<AlgoliaQueueItem>() {
                    new AlgoliaQueueItem
                    {
                        IndexName = Model1.IndexName,
                        Deleted = true,
                        Node = node1
                    },
                    new AlgoliaQueueItem
                    {
                        IndexName = Model2.IndexName,
                        Deleted = false,
                        Node = node2
                    },
                    new AlgoliaQueueItem
                    {
                        IndexName = Model3.IndexName,
                        Deleted = true,
                        Node = node1
                    },
                    new AlgoliaQueueItem
                    {
                        IndexName = Model4.IndexName,
                        Deleted = false,
                        Node = node2
                    }
                };

                var successfulOperations = algoliaIndexingService.ProcessAlgoliaTasks(testQueueItems);

                Assert.AreEqual(testQueueItems.Count, successfulOperations);
            }


            [Test]
            public void ProcessAlgoliaTasks_ListOfQueueItems_WithInvalidIndex_ReturnsProcessedCount()
            {
                var node1 = FakeNodes.GetNode("/Articles/1");
                var node2 = FakeNodes.GetNode("/CZ/Articles/1");
                var testQueueItems = new List<AlgoliaQueueItem>() {
                    new AlgoliaQueueItem
                    {
                        IndexName = Model1.IndexName,
                        Deleted = true,
                        Node = node1
                    },
                    new AlgoliaQueueItem
                    {
                        IndexName = "FAKE_NAME",
                        Deleted = false,
                        Node = node2
                    }
                };

                var successfulOperations = algoliaIndexingService.ProcessAlgoliaTasks(testQueueItems);

                Assert.AreEqual(successfulOperations, 1);
            }
        }
    }
}
