using Algolia.Search.Clients;

using Kentico.Xperience.AlgoliaSearch.Services;

namespace Kentico.Xperience.AlgoliaSearch.Models
{
    /// <summary>
    /// An Algolia index which has been initialized via <see cref="IAlgoliaIndexService.InitializeIndex(string)"/>.
    /// </summary>
    public class InitializedIndex
    {
        /// <summary>
        /// The Algolia index.
        /// </summary>
        public ISearchIndex Index
        {
            get;
        }


        /// <summary>
        /// The name which the index was registered with during startup.
        /// </summary>
        public string Name
        {
            get;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="InitializedIndex"/> class.
        /// </summary>
        /// <param name="index">The Algolia index.</param>
        /// <param name="name">The name of the index.</param>
        public InitializedIndex(ISearchIndex index, string name)
        {
            Index = index;
            Name = name;
        }
    }
}
