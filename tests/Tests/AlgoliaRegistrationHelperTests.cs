using NUnit.Framework;

using System;
using System.Linq;
using System.Reflection;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class AlgoliaRegistrationHelperTests
    {
        [TestFixture]
        internal class GetIndexSettingsTests : AlgoliaTests
        {
            [Test]
            public void GetIndexSettings_EmptyIndexName_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => algoliaRegistrationService.GetIndexSettings(String.Empty));
            }


            [Test]
            public void GetIndexSettings_InvalidIndexName_ReturnsNull()
            {
                var indexSettings = algoliaRegistrationService.GetIndexSettings("FAKE_NAME");
                Assert.IsNull(indexSettings);
            }


            [TestCase(Model1.IndexName, ExpectedResult = new string[] { "DocumentCreatedWhen", "searchable(ClassName)", "filterOnly(DocumentPublishFrom)", "filterOnly(DocumentPublishTo)" })]
            [TestCase(Model2.IndexName, ExpectedResult = new string[] { "filterOnly(Prop1)", "searchable(Prop2)", "searchable(ClassName)", "filterOnly(DocumentPublishFrom)", "filterOnly(DocumentPublishTo)" })]
            [TestCase(Model4.IndexName, ExpectedResult = new string[] { "searchable(ClassName)", "filterOnly(DocumentPublishFrom)", "filterOnly(DocumentPublishTo)" })]
            public string[] GetIndexSettings_AttributesForFaceting_PropertiesConvertedToArray(string indexName)
            {
                return algoliaRegistrationService.GetIndexSettings(indexName).AttributesForFaceting.ToArray();
            }


            [TestCase(Model1.IndexName, ExpectedResult = new string[] { "DocumentCreatedWhen", "ObjectID", "ClassName", "Url" })]
            [TestCase(Model2.IndexName, ExpectedResult = new string[] { "ObjectID", "ClassName", "Url" })]
            [TestCase(Model5.IndexName, ExpectedResult = new string[] { "Prop1", "Prop2", "ObjectID", "ClassName", "Url" })]
            public string[] GetIndexSettings_AttributesToRetrieve_PropertiesConvertedToArray(string indexName)
            {
                return algoliaRegistrationService.GetIndexSettings(indexName).AttributesToRetrieve.ToArray();
            }


            [TestCase(Model1.IndexName, ExpectedResult = new string[] { "DocumentCreatedWhen" })]
            [TestCase(Model2.IndexName, ExpectedResult = new string[] { "unordered(Prop1)", "Prop2" })]
            [TestCase(Model3.IndexName, ExpectedResult = new string[] { "Prop2,Prop3", "Prop1" })]
            [TestCase(Model4.IndexName, ExpectedResult = new string[] { })]
            [TestCase(Model5.IndexName, ExpectedResult = new string[] { "Prop1,Prop2", "Prop3", "unordered(Prop4)", "Prop5", "unordered(Prop6)" })]
            public string[] GetIndexSettings_SearchableAttributes_PropertiesConvertedToArray(string indexName)
            {
                return algoliaRegistrationService.GetIndexSettings(indexName).SearchableAttributes.ToArray();
            }


            [Test]
            public void GetIndexSettings_IndexWithInvalidConfiguration_ThrowsInvalidOperationException()
            {
                Assert.Throws<InvalidOperationException>(() => algoliaRegistrationService.GetIndexSettings(Model6.IndexName));
            }


            [Test]
            public void GetIndexSettings_IndexInheritsBaseClass_ContainsPropertiesFromAllClasses()
            {
                var indexSettings = algoliaRegistrationService.GetIndexSettings(Model7.IndexName);

                Assert.Multiple(() => {
                    Assert.Contains(nameof(Model7.NodeAliasPath), indexSettings.SearchableAttributes);
                    Assert.Contains(nameof(ModelBaseClass.DocumentName), indexSettings.SearchableAttributes);
                });
            }
        }


        [TestFixture]
        internal class IsNodeAlgoliaIndexed : AlgoliaTests
        {
            [TestCase("/Articles/1", ExpectedResult = true)]
            [TestCase("/CZ/Articles/1", ExpectedResult = true)]
            [TestCase("/Articles/1", ExpectedResult = true)]
            [TestCase("/Store/Products/1", ExpectedResult = true)]
            public bool IsNodeAlgoliaIndexed_IndexedNode_ReturnsTrue(string nodeAliasPath)
            {
                var node = FakeNodes.GetNode(nodeAliasPath);
                return algoliaRegistrationService.IsNodeAlgoliaIndexed(node);
            }


            [Test]
            public void IsNodeAlgoliaIndexed_UnindexedNode_ReturnsFalse()
            {
                var node = FakeNodes.GetNode("/Unindexed/Product");
                Assert.IsFalse(algoliaRegistrationService.IsNodeAlgoliaIndexed(node));
            }
        }


        [TestFixture]
        internal class IsNodeIndexedByIndexTests : AlgoliaTests
        {
            [TestCase(Model1.IndexName, "/Articles/1", ExpectedResult = true)]
            [TestCase(Model2.IndexName, "/Articles/1", ExpectedResult = true)]
            [TestCase(Model2.IndexName, "/CZ/Articles/1", ExpectedResult = true)]
            [TestCase(Model3.IndexName, "/Articles/1", ExpectedResult = true)]
            [TestCase(Model4.IndexName, "/Store/Products/1", ExpectedResult = true)]
            public bool IsNodeIndexedByIndex_NodesWithCorrectPath_ReturnsTrue(string indexName, string nodeAliasPath)
            {
                var node = FakeNodes.GetNode(nodeAliasPath);
                return algoliaRegistrationService.IsNodeIndexedByIndex(node, indexName);
            }


            [TestCase(Model1.IndexName, "/CZ/Articles/1", ExpectedResult = false)]
            [TestCase(Model2.IndexName, "/Store/Products/1", ExpectedResult = false)]
            [TestCase(Model3.IndexName, "/CZ/Articles/1", ExpectedResult = false)]
            [TestCase(Model3.IndexName, "/Store/Products/1", ExpectedResult = false)]
            public bool IsNodeIndexedByIndex_NodesWithIncorrectPath_ReturnsFalse(string indexName, string nodeAliasPath)
            {
                var node = FakeNodes.GetNode(nodeAliasPath);
                return algoliaRegistrationService.IsNodeIndexedByIndex(node, indexName);
            }


            [Test]
            public void IsNodeIndexedByIndex_InvalidIndexName_ReturnsFalse()
            {
                var indexName = "FAKE_NAME";
                var node = FakeNodes.GetNode("/Unindexed/Product");
                var isIndexed = algoliaRegistrationService.IsNodeIndexedByIndex(node, indexName);

                var loggedEvent = (eventLogService as MockEventLogService).LoggedEvent;

                Assert.Multiple(() => {
                    Assert.False(isIndexed);
                    Assert.AreEqual(loggedEvent.EventDescription, $"Error loading search model class for index '{indexName}.'");
                });
            }
        }


        [TestFixture]
        internal class GetIndexedColumnNamesTests : AlgoliaTests
        {
            [TestCase(Model1.IndexName, ExpectedResult = new string[] { "DocumentCreatedWhen", "DocumentPublishFrom", "DocumentPublishTo" })]
            public string[] GetIndexedColumnNames_ReturnsDatabaseColumns(string indexName)
            {
                return algoliaRegistrationService.GetIndexedColumnNames(indexName);
            }


            [TestCase(Model2.IndexName, ExpectedResult = new string[] { "Prop2", "DocumentPublishFrom", "DocumentPublishTo", "Column1", "Column2" })]
            public string[] GetIndexedColumnNames_ReturnsSourceColumns(string indexName)
            {
                return algoliaRegistrationService.GetIndexedColumnNames(indexName);
            }
        }


        [TestFixture]
        internal class GetModelByIndexNameTests : AlgoliaTests
        {
            [TestCase(Model1.IndexName, ExpectedResult = typeof(Model1))]
            [TestCase(Model2.IndexName, ExpectedResult = typeof(Model2))]
            [TestCase(Model3.IndexName, ExpectedResult = typeof(Model3))]
            [TestCase(Model4.IndexName, ExpectedResult = typeof(Model4))]
            [TestCase(Model5.IndexName, ExpectedResult = typeof(Model5))]
            [TestCase(Model6.IndexName, ExpectedResult = typeof(Model6))]
            public Type GetModelByIndexName_IndexWithRegisteredName_ReturnsRegisteredClass(string indexName)
            {
                return algoliaRegistrationService.GetModelByIndexName(indexName);
            }


            [Test]
            public void GetModelByIndexName_InvalidIndexName_ReturnsNull()
            {
                var searchModel = algoliaRegistrationService.GetModelByIndexName("FAKE_NAME");
                Assert.IsNull(searchModel);
            }
        }


        [TestFixture]
        internal class RegisterIndexTests : AlgoliaTests
        {
            [Test]
            public void RegisterIndex_EmptyIndexName_LogsErrorAndDoesntRegister()
            {
                var registrationsBefore = algoliaRegistrationService.RegisteredIndexes.Count;
                algoliaRegistrationService.RegisterIndex(String.Empty, typeof(Model1));

                var loggedEvent = (eventLogService as MockEventLogService).LoggedEvent;

                Assert.Multiple(() => {
                    Assert.AreEqual(registrationsBefore, algoliaRegistrationService.RegisteredIndexes.Count);
                    Assert.AreEqual(loggedEvent.EventDescription, "Cannot register Algolia index with empty or null code name.");
                });
            }


            [Test]
            public void RegisterIndex_NullSearchModel_LogsErrorAndDoesntRegister()
            {
                var registrationsBefore = algoliaRegistrationService.RegisteredIndexes.Count;
                algoliaRegistrationService.RegisterIndex("FAKE_NAME", null);

                var loggedEvent = (eventLogService as MockEventLogService).LoggedEvent;

                Assert.Multiple(() => {
                    Assert.AreEqual(registrationsBefore, algoliaRegistrationService.RegisteredIndexes.Count);
                    Assert.AreEqual(loggedEvent.EventDescription, "Cannot register Algolia index with null search model class.");
                });
            }


            [Test]
            public void RegisterIndex_DuplicateIndexName_LogsErrorAndDoesntRegister()
            {
                var registrationsBefore = algoliaRegistrationService.RegisteredIndexes.Count;
                algoliaRegistrationService.RegisterIndex(Model1.IndexName, typeof(Model1));

                var loggedEvent = (eventLogService as MockEventLogService).LoggedEvent;

                Assert.Multiple(() => {
                    Assert.AreEqual(registrationsBefore, algoliaRegistrationService.RegisteredIndexes.Count);
                    Assert.AreEqual(loggedEvent.EventDescription, "Attempted to register Algolia index with name 'Model1,' but it is already registered.");
                });
            }
        }


        [TestFixture]
        internal class RegisteredIndexesTests : AlgoliaTests
        {
            [Test]
            public void RegisteredIndexes_CountMatchesNumberOfIndexes()
            {
                var actualRegistrations = algoliaRegistrationService.GetAlgoliaIndexAttributes(Assembly.GetExecutingAssembly());
                Assert.AreEqual(actualRegistrations.Count(), algoliaRegistrationService.RegisteredIndexes.Count());
            }
        }
    }
}