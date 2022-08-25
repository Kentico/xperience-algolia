using Algolia.Search.Clients;

using Kentico.Xperience.Algolia.KX13.Services;

using NSubstitute;

using NUnit.Framework;

using static Kentico.Xperience.Algolia.KX13.Test.TestSearchModels;

namespace Kentico.Xperience.Algolia.KX13.Test
{
    internal class IAlgoliaIndexRegisterTests
    {
        [TestFixture]
        internal class IndexRegistrationTests : AlgoliaTests
        {
            [Test]
            public void ValidIndex_IsRegistered()
            {
                var indexRegister = new DefaultAlgoliaIndexRegister()
                    .Add<Model8>(Model8.IndexName);

                var algoliaRegistrationService = GetRegistrationService(indexRegister);
                algoliaRegistrationService.RegisterAlgoliaIndexes();

                Assert.AreEqual(8, algoliaRegistrationService.RegisteredIndexes.Count);
            }


            [Test]
            public void DuplicateIndexes_NotRegistered()
            {
                var indexRegister = new DefaultAlgoliaIndexRegister()
                    .Add<Model8>(Model8.IndexName)
                    .Add<Model8>(Model8.IndexName)
                    .Add<Model8>(Model8.IndexName);

                var algoliaRegistrationService = GetRegistrationService(indexRegister);
                algoliaRegistrationService.RegisterAlgoliaIndexes();

                Assert.AreEqual(8, algoliaRegistrationService.RegisteredIndexes.Count);
            }


            [Test]
            public void IndexRegisteredWithAttribute_NotRegistered()
            {
                var indexRegister = new DefaultAlgoliaIndexRegister()
                    .Add<Model7>(Model7.IndexName);

                var algoliaRegistrationService = GetRegistrationService(indexRegister);
                algoliaRegistrationService.RegisterAlgoliaIndexes();

                Assert.AreEqual(7, algoliaRegistrationService.RegisteredIndexes.Count);
            }


            private IAlgoliaRegistrationService GetRegistrationService(IAlgoliaIndexRegister indexRegister)
            {
                return new DefaultAlgoliaRegistrationService(
                    Substitute.For<IAlgoliaSearchService>(),
                    new MockEventLogService(),
                    Substitute.For<ISearchClient>(),
                    Substitute.For<IAlgoliaIndexService>(),
                    indexRegister);
            }
        }
    }
}