using Algolia.Search.Clients;
using CMS;
using CMS.Core;
using Kentico.Xperience.AlgoliaSearch.Services;

[assembly: RegisterImplementation(typeof(IAlgoliaIndexService), typeof(DefaultAlgoliaIndexService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <inheritdoc />
    public class DefaultAlgoliaIndexService : IAlgoliaIndexService
    {
        private readonly ISearchClient searchClient;

        public DefaultAlgoliaIndexService(ISearchClient searchClient)
        {
            this.searchClient = searchClient;
        }

        public ISearchIndex InitializeIndex(string indexName)
        {
            return searchClient.InitIndex(indexName);
        }
    }
}