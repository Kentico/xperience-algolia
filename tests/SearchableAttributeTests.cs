using NUnit.Framework;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    [TestFixture]
    internal class SearchableAttributeTests : AlgoliaTest
    {
        [Test]
        [TestCase(SearchModel1.IndexName, ExpectedResult = new string[] { "prop1,prop2", "prop3", "unordered(prop4)", "prop5", "unordered(prop6)" })]
        public string[] OrderTest(string indexName)
        {
            return AlgoliaSearchHelper.GetIndexSettings(indexName).SearchableAttributes.ToArray();
        }
    }
}
