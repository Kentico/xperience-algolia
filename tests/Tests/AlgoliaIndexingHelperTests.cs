using CMS.DataEngine;
using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Helpers;
using Kentico.Xperience.AlgoliaSearch.Models;

using NUnit.Framework;

using System;
using System.Collections.Generic;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    [TestFixture]
    internal class AlgoliaIndexingHelperTests
    {
        internal class GetTreeNodeDataTests : AlgoliaTest
        {
            [Test]
            public void GetTreeNodeData_UnscheduledNode_ContainsMinMaxPublishingDates()
            {
                var unscheduledNode = FakeNodes.GetNode("/Articles/1");
                var searchModelType = AlgoliaRegistrationHelper.GetModelByIndexName(Model1.IndexName);
                var nodeData = AlgoliaIndexingHelper.GetTreeNodeData(unscheduledNode, searchModelType);

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

                var searchModelType = AlgoliaRegistrationHelper.GetModelByIndexName(Model1.IndexName);
                var nodeData = AlgoliaIndexingHelper.GetTreeNodeData(scheduledNode, searchModelType);

                Assert.Multiple(() => {
                    Assert.AreEqual(nodeData.Value<int>(nameof(AlgoliaSearchModel.DocumentPublishFrom)), scheduledNode.DocumentPublishFrom.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                    Assert.AreEqual(nodeData.Value<int>(nameof(AlgoliaSearchModel.DocumentPublishTo)), scheduledNode.DocumentPublishTo.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                });
            }


            [Test]
            public void GetTreeNodeData_PropertiesFromModel_MatchNodeValue()
            {
                var node = FakeNodes.GetNode("/Articles/1");
                var searchModelType = AlgoliaRegistrationHelper.GetModelByIndexName(Model1.IndexName);
                var nodeData = AlgoliaIndexingHelper.GetTreeNodeData(node, searchModelType);

                Assert.AreEqual(nodeData.Value<DateTime>(nameof(Model1.DocumentCreatedWhen)), new DateTime(2022, 1, 1));
            }


            [Test]
            public void GetTreeNodeData_PropertyWithSource_MatchNodeValue()
            {
                var nodeAliasPath = "/Articles/1";
                var node = FakeNodes.GetNode(nodeAliasPath);
                var searchModelType = AlgoliaRegistrationHelper.GetModelByIndexName(Model4.IndexName);
                var nodeData = AlgoliaIndexingHelper.GetTreeNodeData(node, searchModelType);

                Assert.AreEqual(nodeData.Value<string>(nameof(Model4.Prop1)), nodeAliasPath);
            }


            [Test]
            public void GetTreeNodeData_InheritsBaseClass_ContainsNodeValuesFromAllClasses()
            {
                var node = FakeNodes.GetNode("/Articles/1");
                var data = AlgoliaIndexingHelper.GetTreeNodeData(node, typeof(Model7));

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
                var data = AlgoliaIndexingHelper.GetTreeNodeData(node, typeof(Model7));

                Assert.Multiple(() => {
                    Assert.AreEqual(nodeAliasPath.ToUpper(), data.Value<string>(nameof(Model7.NodeAliasPath)));
                    Assert.AreEqual("NAME", data.Value<string>(nameof(ModelBaseClass.DocumentName)));
                });
            }
        }

        internal class ProcessAlgoliaTasksTests : AlgoliaTest
        {
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

                var successfulOperations = AlgoliaIndexingHelper.ProcessAlgoliaTasks(testQueueItems);
                Assert.AreEqual(testQueueItems.Count, successfulOperations);
            }


            [Test]
            public void ProcessAlgoliaTasks_ListOfQueueItems_WithInvalidIndex_ReturnsProcessedCount()
            {
                var indexName = "FAKE_NAME";
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
                        IndexName = indexName,
                        Deleted = false,
                        Node = node2
                    }
                };

                var successfulOperations = AlgoliaIndexingHelper.ProcessAlgoliaTasks(testQueueItems);
                Assert.Multiple(() => {
                    Assert.AreEqual(successfulOperations, 1);
                    Assert.AreEqual(mEventLogService.LoggedEvent.EventDescription, $"Unable to load search model class for index '{indexName}.'");
                });
            }
        }
    }
}
