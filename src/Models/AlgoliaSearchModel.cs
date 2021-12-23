using Kentico.Xperience.AlgoliaSearch.Attributes;

namespace Kentico.Xperience.AlgoliaSearch.Models
{
    /// <summary>
    /// The base class for all Algolia search models. Contains common Algolia
    /// fields which should be present in all indexes.
    /// </summary>
    public class AlgoliaSearchModel
    {
        /// <summary>
        /// The internal Algolia ID of this search record.
        /// </summary>
        [Retrievable]
        public string ObjectID
        {
            get;
            set;
        }


        /// <summary>
        /// The name of the Xperience class to which the indexed data belongs.
        /// </summary>
        [Retrievable]
        [Facetable]
        public string ClassName
        {
            get;
            set;
        }
    }
}