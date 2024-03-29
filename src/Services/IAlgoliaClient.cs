using System;
using System.Collections.Generic;

using Algolia.Search.Models.Common;

using Kentico.Xperience.Algolia.Models;

using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.Algolia.Services
{
    /// <summary>
    /// Contains methods to interface with the Algolia API.
    /// </summary>
    public interface IAlgoliaClient
    {
        /// <summary>
        /// Removes records from the Algolia index.
        /// </summary>
        /// <param name="objectIds">The Algolia internal IDs of the records to delete.</param>
        /// <param name="indexName">The index containing the objects to delete.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ObjectDisposedException" />
        /// <returns>The number of records deleted.</returns>
        int DeleteRecords(IEnumerable<string> objectIds, string indexName);


        /// <summary>
        /// Gets the indices of the Algolia application with basic statistics.
        /// </summary>
        /// <remarks>See <see href="https://www.algolia.com/doc/api-reference/api-methods/list-indices/#response"/>.</remarks>
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ObjectDisposedException" />
        ICollection<IndicesResponse> GetStatistics();


        /// <summary>
        /// Processes multiple queue items from all Algolia indexes in batches. Algolia
        /// automatically applies batching in multiples of 1,000 when using their API,
        /// so all queue items are forwarded to the API.
        /// </summary>
        /// <param name="items">The items to process.</param>
        /// <returns>The number of items processed.</returns>
        int ProcessAlgoliaTasks(IEnumerable<AlgoliaQueueItem> items);


        /// <summary>
        /// Updates the Algolia index with the dynamic data in each object of the passed <paramref name="dataObjects"/>.
        /// </summary>
        /// <remarks>Logs an error if there are issues loading the node data.</remarks>
        /// <param name="dataObjects">The objects to upsert into Algolia.</param>
        /// <param name="indexName">The index to upsert the data to.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ObjectDisposedException" />
        /// <returns>The number of objects processed.</returns>
        int UpsertRecords(IEnumerable<JObject> dataObjects, string indexName);


        /// <summary>
        /// Rebuilds the Algolia index by removing existing data from Algolia and indexing all
        /// pages in the content tree included in the index.
        /// </summary>
        /// <param name="indexName">The index to rebuild.</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ObjectDisposedException" />
        void Rebuild(string indexName);
    }
}
