using Algolia.Search.Clients;
using Algolia.Search.Models.Settings;

using Kentico.Xperience.AlgoliaSearch.Services;

namespace Kentico.Xperience.AlgoliaSearch.Test.Services
{
    internal class CustomAlgoliaIndexService : IAlgoliaIndexService
    {
        private readonly ISearchClient searchClient;


        public CustomAlgoliaIndexService(ISearchClient searchClient)
        {
            this.searchClient = searchClient;
        }


        public string GetIndexName(string indexName)
        {
            return $"TEST-{indexName}";
        }


        public ISearchIndex InitializeIndex(string indexName)
        {
            return searchClient.InitIndex(indexName);
        }


        public void SetIndexSettings(string indexName, IndexSettings indexSettings)
        {
            var index = InitializeIndex(indexName);
            index.SetSettings(indexSettings);
        }
    }
}