using Algolia.Search.Clients;

using CMS.DocumentEngine;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Creates a connection to the Algolia services and provides methods for updating an Algolia index.
    /// </summary>
    public abstract class AlgoliaConnection
    {
        /// <summary>
        /// Initializes the inner Algolia <see cref="SearchIndex"/> for performing indexing
        /// operations.
        /// </summary>
        /// <param name="indexName">The code name of the Algolia index to manage.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="indexName"/> is empty
        /// or null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the search model is configured
        /// incorrectly or index settings cannot be loaded.</exception>
        public abstract void Initialize(string indexName);


        /// <summary>
        /// Removes records from the Algolia index. Each ID in the list of <paramref name="objectIds"/>
        /// should correspond with the related <see cref="TreeNode.DocumentID"/> of an
        /// Xperience page to remove from the index.
        /// </summary>
        /// <param name="objectIds">The Algolia internal IDs of the records to delete.</param>
        /// <returns>The number of records deleted.</returns>
        public abstract int DeleteRecords(IEnumerable<string> objectIds);


        /// <summary>
        /// Updates the Algolia index with the dynamic data in each object of the passed
        /// <paramref name="dataObjects"/>. To generate the dynamic objects based on the values
        /// of a <see cref="TreeNode"/>, use <see cref="AlgoliaIndexingService.GetTreeNodeData"/>.
        /// </summary>
        /// <remarks>Logs an error if there are issues loading the node data.</remarks>
        /// <param name="dataObjects">The objects to upsert into Algolia.</param>
        /// <returns>The number of objects processed.</returns>
        public abstract int UpsertRecords(IEnumerable<JObject> dataObjects);


        /// <summary>
        /// Rebuilds the Algolia index by removing existing data from Algolia and indexing all
        /// pages in the content tree included in the index.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if a search model class is not
        /// found for the index.</exception>
        public abstract void Rebuild();
    }
}
