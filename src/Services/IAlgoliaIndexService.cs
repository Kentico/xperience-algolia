using Algolia.Search.Clients;
using Algolia.Search.Models.Settings;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Initializes <see cref="ISearchIndex" /> instances.
    /// </summary>
    public interface IAlgoliaIndexService
    {
        /// <summary>
        /// Gets the desired name for the specified index. This can be used to append text to the index
        /// names based on the current environment, e.g. "DEV-Xperience" and "PROD-Xperience."
        /// </summary>
        /// <param name="indexName">The original name of the index.</param>
        /// <returns>The final name of the index.</returns>
        string GetIndexName(string indexName);


        /// <summary>
        /// Initializes a new <see cref="ISearchIndex" /> for the given <paramref name="indexName" />.
        /// </summary>
        /// <param name="indexName">The code name of the index.</param>
        ISearchIndex InitializeIndex(string indexName);


        /// <summary>
        /// Sets the index's settings. See <see href="https://www.algolia.com/doc/api-reference/settings-api-parameters/"/>.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <param name="indexSettings">The index settings.</param>
        void SetIndexSettings(string indexName, IndexSettings indexSettings);
    }
}