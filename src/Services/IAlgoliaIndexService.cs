using Algolia.Search.Clients;

using Kentico.Xperience.AlgoliaSearch.Models;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Initializes <see cref="ISearchIndex" /> instances.
    /// </summary>
    public interface IAlgoliaIndexService
    {
        /// <summary>
        /// Initializes a new <see cref="ISearchIndex" /> for the given <paramref name="indexName" />.
        /// </summary>
        /// <param name="indexName">The name of the index, which may be transformed by this method. The result of
        /// any transformation is returned in the <see cref="InitializedIndex.Name"/> property.</param>
        /// <returns>An <see cref="InitializedIndex"/> containing the Algolia index and the registered name of
        /// the index.</returns>
        InitializedIndex InitializeIndex(string indexName);
    }
}