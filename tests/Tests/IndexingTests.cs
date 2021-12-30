using NUnit.Framework;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    [TestFixture]
    internal class IndexingTests : AlgoliaTest
    {
        [Test]
        [TestCase(Model1.IndexName, "/Articles/1", ExpectedResult = true)]
        [TestCase(Model1.IndexName, "/CZ/Articles/1", ExpectedResult = false)]
        [TestCase(Model2.IndexName, "/Articles/1", ExpectedResult = true)]
        [TestCase(Model2.IndexName, "/CZ/Articles/1", ExpectedResult = true)]
        [TestCase(Model2.IndexName, "/Store/Products/1", ExpectedResult = true)]
        [TestCase(Model3.IndexName, "/Articles/1", ExpectedResult = true)]
        [TestCase(Model3.IndexName, "/CZ/Articles/1", ExpectedResult = false)]
        [TestCase(Model3.IndexName, "/Store/Products/1", ExpectedResult = false)]
        public bool IsNodeIndexedByIndex(string indexName, string nodeAliasPath)
        {
            var node = FakeNodes.GetNode(nodeAliasPath);
            return AlgoliaSearchHelper.IsNodeIndexedByIndex(node, indexName);
        }


        [Test]
        [TestCase(Model1.IndexName, ExpectedResult = new string[] { "Prop1" })]
        [TestCase(Model2.IndexName, ExpectedResult = new string[] { "Prop2", "Column1", "Column2" })]
        public string[] IndexedColumnsMatch(string indexName)
        {
            return AlgoliaSearchHelper.GetIndexedColumnNames(indexName);
        }
    }
}
