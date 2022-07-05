using Algolia.Search.Clients;

using Kentico.Xperience.AlgoliaSearch.Services;
using Kentico.Xperience.AlgoliaSearch.Test.Services;

using NSubstitute;

using NUnit.Framework;

using System.Linq;
using System.Reflection;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class IAlgoliaIndexServiceTests
    {
        [TestFixture]
        internal class GetTreeNodeDataTests : AlgoliaTests
        {
            private IAlgoliaRegistrationService algoliaRegistrationService;
            private IAlgoliaIndexService algoliaIndexService;


            [SetUp]
            public void GetIndexNameTests_SetUp()
            {
                var mockSearchClient = Substitute.For<ISearchClient>();
                algoliaIndexService = new CustomAlgoliaIndexService(mockSearchClient);
                algoliaRegistrationService = new DefaultAlgoliaRegistrationService(Substitute.For<IAlgoliaSearchService>(), new MockEventLogService(), mockSearchClient, algoliaIndexService);

                var attributes = algoliaRegistrationService.GetAlgoliaIndexAttributes(Assembly.GetExecutingAssembly());
                foreach (var attribute in attributes)
                {
                    algoliaRegistrationService.RegisterIndex(attribute);
                }
            }


            [Test]
            public void GetIndexName_CustomImpl_ReturnsCustomName()
            {
                var indexName = algoliaIndexService.GetIndexName(Model1.IndexName);

                Assert.AreEqual($"TEST-{Model1.IndexName}", indexName);
            }


            [Test]
            public void GetIndexName_CustomImpl_CorrectRegisteredNames()
            {
                var registeredIndexNames = algoliaRegistrationService.RegisteredIndexes.Select(i => i.IndexName);

                Assert.Contains($"TEST-{Model1.IndexName}", registeredIndexNames.ToArray());
            }
        }
    }
}
