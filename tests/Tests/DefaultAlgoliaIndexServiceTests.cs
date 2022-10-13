using System;

using Algolia.Search.Clients;

using Kentico.Xperience.Algolia.Models;
using Kentico.Xperience.Algolia.Services;

using NSubstitute;

using NUnit.Framework;

namespace Kentico.Xperience.Algolia.Tests
{
    internal class DefaultAlgoliaIndexServiceTests
    {
        [TestFixture]
        internal class GetIndexSettingsTests : AlgoliaTests
        {
            private DefaultAlgoliaIndexService algoliaIndexService;


            [SetUp]
            public void GetIndexSettingsTestsSetUp()
            {
                algoliaIndexService = new DefaultAlgoliaIndexService(Substitute.For<ISearchClient>());
            }


            [TestCase(typeof(TestSearchModels.ArticleEnSearchModel), ExpectedResult = new string[] { "searchable(FacetableProperty)", "searchable(ClassName)", "filterOnly(DocumentPublishFrom)", "filterOnly(DocumentPublishTo)" })]
            public string[] GetIndexSettings_AttributesForFaceting_PropertiesConvertedToArray(Type searchModel)
            {
                var algoliaIndex = new AlgoliaIndex(searchModel, nameof(searchModel));

                return algoliaIndexService.GetIndexSettings(algoliaIndex).AttributesForFaceting.ToArray();
            }


            [TestCase(typeof(TestSearchModels.ProductsSearchModel), ExpectedResult = new string[] { "RetrievableProperty", "ObjectID", "ClassName", "Url" })]
            public string[] GetIndexSettings_AttributesToRetrieve_PropertiesConvertedToArray(Type searchModel)
            {
                var algoliaIndex = new AlgoliaIndex(searchModel, nameof(searchModel));

                return algoliaIndexService.GetIndexSettings(algoliaIndex).AttributesToRetrieve.ToArray();
            }


            [Test]
            public void GetIndexSettings_DistinctOptions_ReturnsOptions()
            {
                var algoliaIndex = IndexStore.Instance.Get(nameof(TestSearchModels.SplittingModel));
                var indexSettings = algoliaIndexService.GetIndexSettings(algoliaIndex);

                Assert.Multiple(() => {
                    Assert.That(indexSettings.AttributeForDistinct, Is.EqualTo(nameof(TestSearchModels.SplittingModel.AttributeForDistinct)));
                    Assert.That(indexSettings.Distinct, Is.EqualTo(1));
                });
            }


            [TestCase(typeof(TestSearchModels.ArticleEnSearchModel), ExpectedResult = new string[] { "unordered(UnorderedProperty)", "NodeAliasPath" })]
            [TestCase(typeof(TestSearchModels.ProductsSearchModel), ExpectedResult = new string[] { "Order1Property1,Order1Property2", "Order2Property", "NodeAliasPath" })]
            public string[] GetIndexSettings_SearchableAttributes_PropertiesConvertedToArray(Type searchModel)
            {
                var algoliaIndex = new AlgoliaIndex(searchModel, nameof(searchModel));

                return algoliaIndexService.GetIndexSettings(algoliaIndex).SearchableAttributes.ToArray();
            }
        }


        [TestFixture]
        internal class InitializeIndexTests : AlgoliaTests
        {
            private readonly DefaultAlgoliaIndexService algoliaIndexService = new(Substitute.For<ISearchClient>());


            [Test]
            public void InitializeIndex_InvalidIndex_Throws()
            {
                Assert.Throws<InvalidOperationException>(() => algoliaIndexService.InitializeIndex("NO_INDEX"));
            }
        }
    }
}
