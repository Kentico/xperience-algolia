using System;
using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Models
{
    /// <summary>
    /// Represents the configuration of an Algolia index.
    /// </summary>
    public class AlgoliaIndex
    {
        /// <summary>
        /// The name of the attribute used for Algolia de-duplication. See
        /// <see href="https://www.algolia.com/doc/guides/sending-and-managing-data/prepare-your-data/how-to/indexing-long-documents/"/>.
        /// </summary>
        public string DistinctAttribute
        {
            get;
            set;
        }


        /// <summary>
        /// The type of the class which extends <see cref="AlgoliaSearchModel"/>.
        /// </summary>
        public Type Type
        {
            get;
            set;
        }


        /// <summary>
        /// The code name of the Algolia index.
        /// </summary>
        public string IndexName
        {
            get;
            set;
        }


        /// <summary>
        /// The code names of the sites whose pages will be included in the index. If empty, all sites
        /// are included.
        /// </summary>
        public IEnumerable<string> SiteNames
        {
            get;
            set;
        }
    }
}
