using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Models;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Contains methods used during the indexing of content in an Algolia index.
    /// </summary>
    public abstract class IAlgoliaIndexingService
    {
        /// <summary>
        /// Loops through all registered Algolia indexes and logs a task if the passed
        /// <paramref name="node"/> is indexed. For updated pages, a task is only logged
        /// if one of the indexed columns has been modified.
        /// </summary>
        /// <remarks>Logs an error if there are issues loading indexed columns.</remarks>
        /// <param name="node">The <see cref="TreeNode"/> that triggered the event.</param>
        /// <param name="wasDeleted">True if the <paramref name="node"/> was deleted.</param>
        /// <param name="isNew">True if the <paramref name="node"/> was created.</param>
        public abstract void EnqueueAlgoliaItems(TreeNode node, bool wasDeleted, bool isNew);


        /// <summary>
        /// Gets a dynamic <see cref="JObject"/> containing the properties of the Algolia
        /// search model and base class <see cref="AlgoliaSearchModel"/>, populated with data
        /// from the <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> being indexed.</param>
        /// <param name="searchModelType">The class of the Algolia search model.</param>
        /// <returns>A <see cref="JObject"/> with its properties and values set.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="node"/> or
        /// <paramref name="searchModelType"/> are null.</exception>
        public abstract JObject GetTreeNodeData(TreeNode node, Type searchModelType);


        /// <summary>
        /// Processes multiple queue items from all Algolia indexes in batches. Algolia
        /// automatically applies batching in multiples of 1,000 when using their API,
        /// so all queue items are forwarded to the API.
        /// </summary>
        /// <remarks>Logs errors if there are issues initializing the <see cref="AlgoliaConnection"/>.</remarks>
        /// <param name="items">The items to process.</param>
        /// <returns>The number of items processed.</returns>
        public abstract int ProcessAlgoliaTasks(IEnumerable<AlgoliaQueueItem> items);


        /// <summary>
        /// Converts the value from the <paramref name="node"/>'s column from a relative URL
        /// (e.g. ~/getmedia) or an attachment reference into an absolute live-site URL.
        /// </summary>
        /// <remarks>Logs an error if the definition of the <paramref name="columnName"/> can't
        /// be found.</remarks>
        /// <param name="node">The <see cref="TreeNode"/> the value was loaded from.</param>
        /// <param name="nodeValue">The original value of the column.</param>
        /// <param name="columnName">The name of the column the value was loaded from.</param>
        /// <returns>An absolute URL, or null if it couldn't be converted.</returns>
        protected abstract string GetAbsoluteUrlForColumn(TreeNode node, object nodeValue, string columnName);


        /// <summary>
        /// Gets the <paramref name="node"/> value using the <paramref name="property"/>
        /// name, or the property's <see cref="SourceAttribute"/> if specified.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load a value from.</param>
        /// <param name="property">The Algolia search model property.</param>
        /// <param name="searchModelType">The Algolia search model.</param>
        protected abstract object GetNodeValue(TreeNode node, PropertyInfo property, Type searchModelType);


        /// <summary>
        /// Locates the registered search model properties which match the property names of the passed
        /// <paramref name="node"/> and sets the <paramref name="data"/> values from the <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load values from.</param>
        /// <param name="data">The dynamic data that will be passed to Algolia.</param>
        /// <param name="searchModelType">The class of the Algolia search model.</param>
        protected abstract void MapTreeNodeProperties(TreeNode node, JObject data, Type searchModelType);


        /// <summary>
        /// Sets values in the <paramref name="data"/> object using the common search model properties
        /// located within the <see cref="AlgoliaSearchModel"/> class.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load values from.</param>
        /// <param name="data">The dynamic data that will be passed to Algolia.</param>
        protected abstract void MapCommonProperties(TreeNode node, JObject data);
    }
}
