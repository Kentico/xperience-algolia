﻿using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Attributes;

using Newtonsoft.Json.Linq;

using System.Collections.Generic;

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
        [Facetable(Searchable = true)]
        public string ClassName
        {
            get;
            set;
        }


        /// <summary>
        /// The <see cref="TreeNode.DocumentPublishFrom"/> value which is automatically
        /// converted to a Unix timestamp in UTC.
        /// </summary>
        [Facetable(FilterOnly = true)]
        public int DocumentPublishFrom
        {
            get;
            set;
        }


        /// <summary>
        /// The <see cref="TreeNode.DocumentPublishTo"/> value which is automatically
        /// converted to a Unix timestamp in UTC.
        /// </summary>
        [Facetable(FilterOnly = true)]
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


        /// <summary>
        /// Called when indexing a search model property. Does not trigger when indexing the
        /// properties specified by <see cref="AlgoliaSearchModel"/>.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> currently being indexed.</param>
        /// <param name="propertyName">The search model property that is being indexed.</param>
        /// <param name="usedColumn">The column that the value was retrieved from when the
        /// property uses the <see cref="SourceAttribute"/>. If not used, the parameter will
        /// be null.</param>
        /// <param name="foundValue">The value of the property that was found in the <paramref name="node"/>,
        /// or null if no value was found.</param>
        /// <returns>The value that will be indexed in Algolia.</returns>
        public virtual object OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
        {
            return foundValue;
        }


        /// <summary>
        /// Called when indexing a <see cref="TreeNode"/> if the <see cref="AlgoliaIndex"/> was registered
        /// with a <see cref="AlgoliaIndex.DistinctAttribute"/> and <see cref="AlgoliaIndex.DistinctLevel"/>.
        /// This method can be used to split one large Algolia record into multiple, smaller records.
        /// </summary>
        /// <remarks>See <see href="https://www.algolia.com/doc/guides/sending-and-managing-data/prepare-your-data/how-to/indexing-long-documents/"/>.</remarks>
        /// <param name="originalData">The node's data before being split into smaller objects.</param>
        /// <returns>One or more <see cref="JObject"/>s representing the data of the node.</returns>
        public virtual IEnumerable<JObject> SplitData(JObject originalData)
        {
            return new JObject[] { originalData };
        }
    }
}