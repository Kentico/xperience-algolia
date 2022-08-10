﻿using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Models;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Contains methods used during the indexing of content in an Algolia index.
    /// </summary>
    public interface IAlgoliaIndexingService
    {
        /// <summary>
        /// Loops through all registered Algolia indexes and logs a task if the passed
        /// <paramref name="node"/> is indexed. For updated pages, a task is only logged
        /// if one of the indexed columns has been modified.
        /// </summary>
        /// <remarks>Logs an error if there are issues loading indexed columns.</remarks>
        /// <param name="node">The <see cref="TreeNode"/> that triggered the event.</param>
        /// <param name="eventName">The name of the Xperience event that was triggered.</param>
        void EnqueueAlgoliaItems(TreeNode node, string eventName);


        /// <summary>
        /// Gets dynamic <see cref="JObject"/>s containing the properties of the Algolia
        /// search model and base class <see cref="AlgoliaSearchModel"/>, populated with data
        /// from the <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> being indexed.</param>
        /// <param name="algoliaIndex">The Algolia index which includes the <paramref name="node"/>.</param>
        /// <returns>One or more <see cref="JObject"/>s representing the data of the <paramref name="node"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="node"/> or
        /// <paramref name="algoliaIndex"/> are null.</exception>
        IEnumerable<JObject> GetTreeNodeData(TreeNode node, AlgoliaIndex algoliaIndex);


        /// <summary>
        /// Processes multiple queue items from all Algolia indexes in batches. Algolia
        /// automatically applies batching in multiples of 1,000 when using their API,
        /// so all queue items are forwarded to the API.
        /// </summary>
        /// <remarks>Logs errors if there are issues initializing the <see cref="IAlgoliaConnection"/>.</remarks>
        /// <param name="items">The items to process.</param>
        /// <returns>The number of items processed.</returns>
        int ProcessAlgoliaTasks(IEnumerable<AlgoliaQueueItem> items);
    }
}
