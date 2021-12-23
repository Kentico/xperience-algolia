using CMS.DocumentEngine;

using Tests.DocumentEngine;

using NUnit.Framework;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    [TestFixture]
    internal class IndexingTests : AlgoliaTest
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            // Register document types for faking
            DocumentGenerator.RegisterDocumentType<TreeNode>(FakeNodes.DOCTYPE_ARTICLE);
            DocumentGenerator.RegisterDocumentType<TreeNode>(FakeNodes.DOCTYPE_PRODUCT);
            Fake().DocumentType<TreeNode>(FakeNodes.DOCTYPE_ARTICLE);
            Fake().DocumentType<TreeNode>(FakeNodes.DOCTYPE_PRODUCT);

            FakeNodes.MakeNode("/Articles/1", FakeNodes.DOCTYPE_ARTICLE);
            FakeNodes.MakeNode("/CZ/Articles/1", FakeNodes.DOCTYPE_ARTICLE, "cs-CZ");
            FakeNodes.MakeNode("/Store/Products/1", FakeNodes.DOCTYPE_PRODUCT);
            FakeNodes.MakeNode("/CZ/Store/Products/2", FakeNodes.DOCTYPE_PRODUCT, "cs-CZ");
        }


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
    }
}
