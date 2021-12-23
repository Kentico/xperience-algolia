using NUnit.Framework;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    [TestFixture]
    internal class AttributeTests : AlgoliaTest
    {
        [Test]
        [TestCase(Model1.IndexName, ExpectedResult = new string[] { "prop1", "className" })]
        [TestCase(Model2.IndexName, ExpectedResult = new string[] { "prop1", "prop2", "className" })]
        [TestCase(Model4.IndexName, ExpectedResult = new string[] { "className" })]
        public string[] FacetableAttributesConvertedToAlgoliaFormat(string indexName)
        {
            return AlgoliaSearchHelper.GetIndexSettings(indexName).AttributesForFaceting.ToArray();
        }


        [Test]
        [TestCase(Model1.IndexName, ExpectedResult = new string[] { "prop1", "objectID", "className" })]
        [TestCase(Model2.IndexName, ExpectedResult = new string[] { "objectID", "className" })]
        [TestCase(Model5.IndexName, ExpectedResult = new string[] { "prop1", "prop2", "objectID", "className" })]
        public string[] RetrievableAttributesConvertedToAlgoliaFormat(string indexName)
        {
            return AlgoliaSearchHelper.GetIndexSettings(indexName).AttributesToRetrieve.ToArray();
        }


        [Test]
        [TestCase(Model1.IndexName, ExpectedResult = new string[] { "prop1" })]
        [TestCase(Model2.IndexName, ExpectedResult = new string[] { "prop1", "prop2" })]
        [TestCase(Model3.IndexName, ExpectedResult = new string[] { "prop2,prop3", "prop1" })]
        [TestCase(Model4.IndexName, ExpectedResult = new string[] { })]
        [TestCase(Model5.IndexName, ExpectedResult = new string[] { "prop1,prop2", "prop3", "unordered(prop4)", "prop5", "unordered(prop6)" })]
        public string[] SearchableAttributesConvertedToAlgoliaFormat(string indexName)
        {
            return AlgoliaSearchHelper.GetIndexSettings(indexName).SearchableAttributes.ToArray();
        }
    }
}