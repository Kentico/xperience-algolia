using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Attributes;

using System;

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
        [Facetable(searchable: true)]
        public string ClassName
        {
            get;
            set;
        }


        /// <summary>
        /// The <see cref="TreeNode.DocumentPublishFrom"/> value which is automatically
        /// converted to a Unix timestamp in UTC.
        /// </summary>
        [Facetable(filterOnly: true)]
        public int DocumentPublishFrom
        {
            get;
            set;
        }


        /// <summary>
        /// The <see cref="TreeNode.DocumentPublishTo"/> value which is automatically
        /// converted to a Unix timestamp in UTC.
        /// </summary>
        [Facetable(filterOnly: true)]
        public int DocumentPublishTo
        {
            get;
            set;
        }


        /// <summary>
        /// The absolute live site URL of the indexed page.
        /// </summary>
        [Retrievable]
        public string Url
        {
            get;
            set;
        }
    }
}