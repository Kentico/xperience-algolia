using CMS.Core;

using Kentico.Xperience.AlgoliaSearch.Helpers;

using NUnit.Framework;

using System;
using System.Linq;
using System.Reflection;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    [TestFixture]
    internal class AlgoliaRegistrationHelperTests
    {
        internal class GetIndexSettingsTests : AlgoliaTest
        {
            [Test]
            public void GetIndexSettings_EmptyIndexName_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => AlgoliaRegistrationHelper.GetIndexSettings(String.Empty));
            }


            [Test]
            public void GetIndexSettings_InvalidIndexName_ReturnsNull()
            {
                var indexSettings = AlgoliaRegistrationHelper.GetIndexSettings("FAKE_NAME");
                Assert.IsNull(indexSettings);
            }


            [TestCase(Model1.IndexName, ExpectedResult = new string[] { "DocumentCreatedWhen", "searchable(ClassName)", "filterOnly(DocumentPublishFrom)", "filterOnly(DocumentPublishTo)" })]
            [TestCase(Model2.IndexName, ExpectedResult = new string[] { "filterOnly(Prop1)", "searchable(Prop2)", "searchable(ClassName)", "filterOnly(DocumentPublishFrom)", "filterOnly(DocumentPublishTo)" })]
            [TestCase(Model4.IndexName, ExpectedResult = new string[] { "searchable(ClassName)", "filterOnly(DocumentPublishFrom)", "filterOnly(DocumentPublishTo)" })]
            public string[] GetIndexSettings_AttributesForFaceting_PropertiesConvertedToArray(string indexName)
            {
                return AlgoliaRegistrationHelper.GetIndexSettings(indexName).AttributesForFaceting.ToArray();
            }


            [TestCase(Model1.IndexName, ExpectedResult = new string[] { "DocumentCreatedWhen", "ObjectID", "ClassName", "Url" })]
            [TestCase(Model2.IndexName, ExpectedResult = new string[] { "ObjectID", "ClassName", "Url" })]
            [TestCase(Model5.IndexName, ExpectedResult = new string[] { "Prop1", "Prop2", "ObjectID", "ClassName", "Url" })]
            public string[] GetIndexSettings_AttributesToRetrieve_PropertiesConvertedToArray(string indexName)
            {
                return AlgoliaRegistrationHelper.GetIndexSettings(indexName).AttributesToRetrieve.ToArray();
            }


            [TestCase(Model1.IndexName, ExpectedResult = new string[] { "DocumentCreatedWhen" })]
            [TestCase(Model2.IndexName, ExpectedResult = new string[] { "unordered(Prop1)", "Prop2" })]
            [TestCase(Model3.IndexName, ExpectedResult = new string[] { "Prop2,Prop3", "Prop1" })]
            [TestCase(Model4.IndexName, ExpectedResult = new string[] { })]
            [TestCase(Model5.IndexName, ExpectedResult = new string[] { "Prop1,Prop2", "Prop3", "unordered(Prop4)", "Prop5", "unordered(Prop6)" })]
            public string[] GetIndexSettings_SearchableAttributes_PropertiesConvertedToArray(string indexName)
            {
                return AlgoliaRegistrationHelper.GetIndexSettings(indexName).SearchableAttributes.ToArray();
            }


            [Test]
            public void GetIndexSettings_IndexWithInvalidConfiguration_ThrowsInvalidOperationException()
            {
                Assert.Throws<InvalidOperationException>(() => AlgoliaRegistrationHelper.GetIndexSettings(Model6.IndexName));
            }
        }


        internal class IsNodeAlgoliaIndexed : AlgoliaTest
        {
            [TestCase("/Articles/1", ExpectedResult = true)]
            [TestCase("/CZ/Articles/1", ExpectedResult = true)]
            [TestCase("/Articles/1", ExpectedResult = true)]
            [TestCase("/Store/Products/1", ExpectedResult = true)]
            public bool IsNodeAlgoliaIndexed_IndexedNode_ReturnsTrue(string nodeAliasPath)
            {
                var node = FakeNodes.GetNode(nodeAliasPath);
                return AlgoliaRegistrationHelper.IsNodeAlgoliaIndexed(node);
            }


            [Test]
            public void IsNodeAlgoliaIndexed_UnindexedNode_ReturnsFalse()
            {
                var node = FakeNodes.GetNode("/Unindexed/Product");
                Assert.IsFalse(AlgoliaRegistrationHelper.IsNodeAlgoliaIndexed(node));
            }
        }


        internal class IsNodeIndexedByIndexTests : AlgoliaTest
        {
            [TestCase(Model1.IndexName, "/Articles/1", ExpectedResult = true)]
            [TestCase(Model2.IndexName, "/Articles/1", ExpectedResult = true)]
            [TestCase(Model2.IndexName, "/CZ/Articles/1", ExpectedResult = true)]
            [TestCase(Model3.IndexName, "/Articles/1", ExpectedResult = true)]
            [TestCase(Model4.IndexName, "/Store/Products/1", ExpectedResult = true)]
            public bool IsNodeIndexedByIndex_NodesWithCorrectPath_ReturnsTrue(string indexName, string nodeAliasPath)
            {
                var node = FakeNodes.GetNode(nodeAliasPath);
                return AlgoliaRegistrationHelper.IsNodeIndexedByIndex(node, indexName);
            }


            [TestCase(Model1.IndexName, "/CZ/Articles/1", ExpectedResult = false)]
            [TestCase(Model2.IndexName, "/Store/Products/1", ExpectedResult = false)]
            [TestCase(Model3.IndexName, "/CZ/Articles/1", ExpectedResult = false)]
            [TestCase(Model3.IndexName, "/Store/Products/1", ExpectedResult = false)]
            public bool IsNodeIndexedByIndex_NodesWithIncorrectPath_ReturnsFalse(string indexName, string nodeAliasPath)
            {
                var node = FakeNodes.GetNode(nodeAliasPath);
                return AlgoliaRegistrationHelper.IsNodeIndexedByIndex(node, indexName);
            }


            [Test]
            public void IsNodeIndexedByIndex_InvalidIndexName_ReturnsFalse()
            {
                var indexName = "FAKE_NAME";
                var node = FakeNodes.GetNode("/Unindexed/Product");
                var isIndexed = AlgoliaRegistrationHelper.IsNodeIndexedByIndex(node, indexName);

                Assert.Multiple(() => {
                    Assert.False(isIndexed);
                    Assert.AreEqual(mEventLogService.LoggedEvent.EventDescription, $"Error loading search model class for index '{indexName}.'");
                });
            }
        }


        internal class GetIndexedColumnNamesTests : AlgoliaTest
        {
            [TestCase(Model1.IndexName, ExpectedResult = new string[] { "DocumentCreatedWhen", "DocumentPublishFrom", "DocumentPublishTo" })]
            public string[] GetIndexedColumnNames_ReturnsDatabaseColumns(string indexName)
            {
                return AlgoliaRegistrationHelper.GetIndexedColumnNames(indexName);
            }


            [TestCase(Model2.IndexName, ExpectedResult = new string[] { "Prop2", "DocumentPublishFrom", "DocumentPublishTo", "Column1", "Column2" })]
            public string[] GetIndexedColumnNames_ReturnsSourceColumns(string indexName)
            {
                return AlgoliaRegistrationHelper.GetIndexedColumnNames(indexName);
            }
        }


        internal class GetModelByIndexNameTests : AlgoliaTest
        {
            [TestCase(Model1.IndexName, ExpectedResult = typeof(Model1))]
            [TestCase(Model2.IndexName, ExpectedResult = typeof(Model2))]
            [TestCase(Model3.IndexName, ExpectedResult = typeof(Model3))]
            [TestCase(Model4.IndexName, ExpectedResult = typeof(Model4))]
            [TestCase(Model5.IndexName, ExpectedResult = typeof(Model5))]
            [TestCase(Model6.IndexName, ExpectedResult = typeof(Model6))]
            public Type GetModelByIndexName_IndexWithRegisteredName_ReturnsRegisteredClass(string indexName)
            {
                return AlgoliaRegistrationHelper.GetModelByIndexName(indexName);
            }


            [Test]
            public void GetModelByIndexName_InvalidIndexName_ReturnsNull()
            {
                var searchModel = AlgoliaRegistrationHelper.GetModelByIndexName("FAKE_NAME");
                Assert.IsNull(searchModel);
            }
        }


        internal class RegisterIndexTests : AlgoliaTest
        {
            [Test]
            public void RegisterIndex_EmptyIndexName_LogsErrorAndDoesntRegister()
            {
                var registrationsBefore = AlgoliaRegistrationHelper.RegisteredIndexes.Count;
                AlgoliaRegistrationHelper.RegisterIndex(String.Empty, typeof(Model1));

                Assert.Multiple(() => {
                    Assert.AreEqual(registrationsBefore, AlgoliaRegistrationHelper.RegisteredIndexes.Count);
                    Assert.AreEqual(mEventLogService.LoggedEvent.EventDescription, "Cannot register Algolia index with empty or null code name.");
                });
            }


            [Test]
            public void RegisterIndex_NullSearchModel_LogsErrorAndDoesntRegister()
            {
                var registrationsBefore = AlgoliaRegistrationHelper.RegisteredIndexes.Count;
                AlgoliaRegistrationHelper.RegisterIndex("FAKE_NAME", null);

                Assert.Multiple(() => {
                    Assert.AreEqual(registrationsBefore, AlgoliaRegistrationHelper.RegisteredIndexes.Count);
                    Assert.AreEqual(mEventLogService.LoggedEvent.EventDescription, "Cannot register Algolia index with null search model class.");
                });
            }


            [Test]
            public void RegisterIndex_DuplicateIndexName_LogsErrorAndDoesntRegister()
            {
                var registrationsBefore = AlgoliaRegistrationHelper.RegisteredIndexes.Count;
                AlgoliaRegistrationHelper.RegisterIndex(Model1.IndexName, typeof(Model1));

                Assert.Multiple(() => {
                    Assert.AreEqual(registrationsBefore, AlgoliaRegistrationHelper.RegisteredIndexes.Count);
                    Assert.AreEqual(mEventLogService.LoggedEvent.EventDescription, "Attempted to register Algolia index with name 'Model1,' but it is already registered.");
                });
            }
        }


        internal class RegisteredIndexesTests : AlgoliaTest
        {
            [Test]
            public void RegisteredIndexes_CountMatchesNumberOfIndexes()
            {
                var actualRegistrations = AlgoliaRegistrationHelper.GetAlgoliaIndexAttributes(Assembly.GetExecutingAssembly());
                Assert.AreEqual(actualRegistrations.Count(), AlgoliaRegistrationHelper.RegisteredIndexes.Count());
            }
        }
    }
}