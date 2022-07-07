using Algolia.Search.Clients;

using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.SiteProvider;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Services;

using NSubstitute;

using NUnit.Framework;

using System;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class IAlgoliaRegistrationServiceTests
    {
        [TestFixture]
        internal class GetIndexSettingsTests : AlgoliaTests
        {
            private IAlgoliaRegistrationService algoliaRegistrationService;


            [SetUp]
            public void GetIndexSettingsTests_SetUp()
            {
                var mockSearchClient = Substitute.For<ISearchClient>();
                var mockalgoliaIndexService = Substitute.For<IAlgoliaIndexService>();
                mockalgoliaIndexService.InitializeIndex(Arg.Any<string>()).Returns(args => new InitializedIndex(Substitute.For<ISearchIndex>(), args.Arg<string>()));

                var algoliaSearchService = new DefaultAlgoliaSearchService(mockSearchClient, Substitute.For<IAppSettingsService>());
                algoliaRegistrationService = new DefaultAlgoliaRegistrationService(algoliaSearchService, new MockEventLogService(), mockSearchClient, mockalgoliaIndexService);
                algoliaRegistrationService.RegisterAlgoliaIndexes();
            }


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
            private IAlgoliaRegistrationService algoliaRegistrationService;


            [SetUp]
            public void IsNodeAlgoliaIndexed_SetUp()
            {
                var mockSearchClient = Substitute.For<ISearchClient>();
                var mockalgoliaIndexService = Substitute.For<IAlgoliaIndexService>();
                mockalgoliaIndexService.InitializeIndex(Arg.Any<string>()).Returns(args => new InitializedIndex(Substitute.For<ISearchIndex>(), args.Arg<string>()));

                algoliaRegistrationService = new DefaultAlgoliaRegistrationService(Substitute.For<IAlgoliaSearchService>(), new MockEventLogService(), mockSearchClient, mockalgoliaIndexService);
                algoliaRegistrationService.RegisterAlgoliaIndexes();
            }


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
            private IAlgoliaRegistrationService algoliaRegistrationService;


            [SetUp]
            public void IsNodeIndexedByIndexTests_SetUp()
            {
                var mockSearchClient = Substitute.For<ISearchClient>();
                var mockalgoliaIndexService = Substitute.For<IAlgoliaIndexService>();
                mockalgoliaIndexService.InitializeIndex(Arg.Any<string>()).Returns(args => new InitializedIndex(Substitute.For<ISearchIndex>(), args.Arg<string>()));

                algoliaRegistrationService = new DefaultAlgoliaRegistrationService(Substitute.For<IAlgoliaSearchService>(), new MockEventLogService(), mockSearchClient, mockalgoliaIndexService);
                algoliaRegistrationService.RegisterAlgoliaIndexes();
            }


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

                Assert.False(algoliaRegistrationService.IsNodeIndexedByIndex(node, indexName));
            }


            [Test]
            public void IsNodeIndexedByIndex_CorrectSiteName_ReturnsTrue()
            {
                var node = FakeNodes.GetNode("/CZ/Articles/1");

                Assert.True(algoliaRegistrationService.IsNodeIndexedByIndex(node, Model2.IndexName));
            }


            [Test]
            public void IsNodeIndexedByIndex_NodeOnDifferentSite_ReturnsFalse()
            {
                var nonDefaultSite = SiteInfo.Provider.Get(FAKE_SITE);
                var nodeOnDifferentSite = TreeNode.New(FakeNodes.DOCTYPE_ARTICLE).With(p =>
                {
                    p.SetValue("NodeSiteID", nonDefaultSite.SiteID);
                });

                Assert.False(algoliaRegistrationService.IsNodeIndexedByIndex(nodeOnDifferentSite, Model2.IndexName));
            }
        }


        [TestFixture]
        internal class GetIndexedColumnNamesTests : AlgoliaTests
        {
            private IAlgoliaRegistrationService algoliaRegistrationService;


            [SetUp]
            public void GetIndexedColumnNamesTests_SetUp()
            {
                var mockalgoliaIndexService = Substitute.For<IAlgoliaIndexService>();
                mockalgoliaIndexService.InitializeIndex(Arg.Any<string>()).Returns(args => new InitializedIndex(Substitute.For<ISearchIndex>(), args.Arg<string>()));

                algoliaRegistrationService = new DefaultAlgoliaRegistrationService(Substitute.For<IAlgoliaSearchService>(), new MockEventLogService(), Substitute.For<ISearchClient>(), mockalgoliaIndexService);
                algoliaRegistrationService.RegisterAlgoliaIndexes();
            }


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
        internal class RegisterIndexTests : AlgoliaTests
        {
            private IAlgoliaRegistrationService algoliaRegistrationService;


            [SetUp]
            public void RegisterIndexTests_SetUp()
            {
                var mockSearchClient = Substitute.For<ISearchClient>();
                var algoliaIndexService = new DefaultAlgoliaIndexService(mockSearchClient);
                algoliaRegistrationService = new DefaultAlgoliaRegistrationService(Substitute.For<IAlgoliaSearchService>(), new MockEventLogService(), mockSearchClient, algoliaIndexService);
            }


            [Test]
            public void RegisterIndex_ValidIndexes_AreRegistered()
            {
                algoliaRegistrationService.RegisterIndex(new RegisterAlgoliaIndexAttribute(typeof(Model1), Model1.IndexName));
                algoliaRegistrationService.RegisterIndex(new RegisterAlgoliaIndexAttribute(typeof(Model2), Model2.IndexName));
                algoliaRegistrationService.RegisterIndex(new RegisterAlgoliaIndexAttribute(typeof(Model3), Model3.IndexName));

                Assert.AreEqual(3, algoliaRegistrationService.RegisteredIndexes.Count);
            }


            [Test]
            public void RegisterIndex_EmptyIndexName_DoesntRegisterIndex()
            {
                algoliaRegistrationService.RegisterIndex(new RegisterAlgoliaIndexAttribute(typeof(Model1), String.Empty));

                Assert.AreEqual(0, algoliaRegistrationService.RegisteredIndexes.Count);
            }


            [Test]
            public void RegisterIndex_NullSearchModel_DoesntRegisterIndex()
            {
                algoliaRegistrationService.RegisterIndex(new RegisterAlgoliaIndexAttribute(null, "FAKE_NAME"));

                Assert.AreEqual(0, algoliaRegistrationService.RegisteredIndexes.Count);
            }


            [Test]
            public void RegisterIndex_DuplicateIndexName_DoesntRegisterIndex()
            {
                algoliaRegistrationService.RegisterIndex(new RegisterAlgoliaIndexAttribute(typeof(Model1), Model1.IndexName));
                algoliaRegistrationService.RegisterIndex(new RegisterAlgoliaIndexAttribute(typeof(Model1), Model1.IndexName));

                Assert.AreEqual(1, algoliaRegistrationService.RegisteredIndexes.Count);
            }
        }
    }
}