using CMS;
using CMS.DocumentEngine;
using CMS.SiteProvider;
using CMS.Tests;

using NUnit.Framework;

using Tests.DocumentEngine;

[assembly: Category("Algolia")]
[assembly: AssemblyDiscoverable]
namespace Kentico.Xperience.Algolia.KX13.Test
{
    internal class AlgoliaTests : UnitTests
    {
        public const string DEFAULT_SITE = "TestSite";
        public const string FAKE_SITE = "FAKE_SITE";


        [SetUp]
        public void SetUp()
        {
            Fake<SiteInfo, SiteInfoProvider>().WithData(
            new SiteInfo
            {
                SiteName = DEFAULT_SITE
            },
            new SiteInfo
            {
                SiteName = FAKE_SITE
            }
            );

            // Register document types for faking
            DocumentGenerator.RegisterDocumentType<TreeNode>(FakeNodes.DOCTYPE_ARTICLE);
            DocumentGenerator.RegisterDocumentType<TreeNode>(FakeNodes.DOCTYPE_PRODUCT);
            Fake().DocumentType<TreeNode>(FakeNodes.DOCTYPE_ARTICLE);
            Fake().DocumentType<TreeNode>(FakeNodes.DOCTYPE_PRODUCT);

            // Create TreeNodes
            FakeNodes.MakeNode("/Articles/1", FakeNodes.DOCTYPE_ARTICLE);
            FakeNodes.MakeNode("/CZ/Articles/1", FakeNodes.DOCTYPE_ARTICLE, "cs-CZ");
            FakeNodes.MakeNode("/Store/Products/1", FakeNodes.DOCTYPE_PRODUCT);
            FakeNodes.MakeNode("/CZ/Store/Products/2", FakeNodes.DOCTYPE_PRODUCT, "cs-CZ");
            FakeNodes.MakeNode("/Unindexed/Product", FakeNodes.DOCTYPE_PRODUCT);
            FakeNodes.MakeNode("/Scheduled/Article", FakeNodes.DOCTYPE_ARTICLE);
        }


        [TearDown]
        public void TearDown()
        {
            FakeNodes.ClearNodes();
        }
    }
}
