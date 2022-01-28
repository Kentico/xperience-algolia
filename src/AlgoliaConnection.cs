using Algolia.Search.Clients;

using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Helpers;
using Kentico.Xperience.AlgoliaSearch.Models;

using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Creates a connection to the Algolia services and provides methods for updating an Algolia index.
    /// The columns that are indexed are dependent on the search model class registered with the Algolia
    /// index code name during startup via the <see cref="RegisterAlgoliaIndexAttribute"/> attribute.
    /// </summary>
    public class AlgoliaConnection
    {
        private Type searchModelType;
        private SearchIndex searchIndex;


        /// <summary>
        /// Initializes the inner Algolia <see cref="SearchIndex"/> for performing indexing
        /// operations.
        /// </summary>
        /// <param name="indexName">The code name of the Algolia index to manage.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="indexName"/> is empty
        /// or null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the search model is configured
        /// incorrectly or index settings cannot be loaded.</exception>
        public AlgoliaConnection(string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(indexName);
            }

            var configuration = Service.ResolveOptional<IConfiguration>();
            var client = AlgoliaSearchHelper.GetSearchClient(configuration);

            searchIndex = client.InitIndex(indexName);
            searchModelType = AlgoliaRegistrationHelper.GetModelByIndexName(indexName);

            if (searchModelType == null)
            {
                throw new InvalidOperationException($"Unable to load search model class for index '{indexName}.'");
            }

            if (!searchModelType.IsSubclassOf(typeof(AlgoliaSearchModel)))
            {
                throw new InvalidOperationException($"Algolia search models must extend the {nameof(AlgoliaSearchModel)} class.");
            }
        }


        /// <summary>
        /// Removes records from the Algolia index. The <see cref="TreeNode.DocumentID"/> of
        /// each <see cref="TreeNode"/> is used to reference the internal Algolia object ID
        /// to delete.
        /// </summary>
        /// <param name="nodes">The <see cref="TreeNode"/>s to delete.</param>
        /// <returns>The number of nodes processed.</returns>
        public int DeleteTreeNodes(IEnumerable<TreeNode> nodes)
        {
            var deletedCount = 0;
            if (nodes == null || nodes.Count() == 0)
            {
                return 0;
            }

            var documentIds = nodes.Select(node => node.DocumentID.ToString());
            var responses = searchIndex.DeleteObjects(documentIds).Responses;
            foreach (var response in responses)
            {
                deletedCount += response.ObjectIDs.Count();
            }

            return deletedCount;
        }


        /// <summary>
        /// Updates the Algolia index with the property values that are specified in the
        /// registered search model class for each <see cref="TreeNode"/> in the <paramref name="nodes"/>
        /// collection. The internal Algolia object IDs are set to the <see cref="TreeNode.DocumentID"/>
        /// of each <see cref="TreeNode"/>.
        /// </summary>
        /// <remarks>Logs an error if there are issues loading the node data.</remarks>
        /// <param name="nodes">The <see cref="TreeNode"/>s to load property values from.</param>
        /// <returns>The number of nodes processed.</returns>
        public int UpsertTreeNodes(IEnumerable<TreeNode> nodes)
        {
            var upsertedCount = 0;
            if (nodes == null || nodes.Count() == 0)
            {
                return 0;
            }

            try
            {
                var dataObjects = new List<JObject>();
                foreach (var node in nodes)
                {
                    var data = AlgoliaIndexingHelper.GetTreeNodeData(node, searchModelType);
                    dataObjects.Add(data);
                }
                
                var responses = searchIndex.SaveObjects(dataObjects).Responses;
                foreach (var response in responses)
                {
                    upsertedCount += response.ObjectIDs.Count();
                }

                return upsertedCount;
            }
            catch (ArgumentNullException ex)
            {
                LogError(nameof(UpsertTreeNodes), ex.Message);
                return upsertedCount;
            }
        }


        /// <summary>
        /// Rebuilds the Algolia index by removing existing data from Algolia and indexing all
        /// pages in the content tree included in the index.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if a search model class is not
        /// found for the index.</exception>
        public void Rebuild()
        {
            if (searchModelType == null)
            {
                throw new InvalidOperationException("No registered search model class found for index.");
            }

            searchIndex.ClearObjects();

            var indexedNodes = new List<TreeNode>();
            var includedPathAttributes = searchModelType.GetCustomAttributes<IncludedPathAttribute>(false);
            foreach (var includedPathAttribute in includedPathAttributes)
            {
                var query = new MultiDocumentQuery()
                    .OnCurrentSite()
                    .Path(includedPathAttribute.AliasPath)
                    .PublishedVersion()
                    .WithCoupledColumns();

                if (includedPathAttribute.PageTypes.Length > 0)
                {
                    query.Types(includedPathAttribute.PageTypes);
                }

                if (includedPathAttribute.Cultures.Length > 0)
                {
                    query.Culture(includedPathAttribute.Cultures);
                }

                indexedNodes.AddRange(query.TypedResult);
            }

            UpsertTreeNodes(indexedNodes);
        }


        private void LogError(string code, string message)
        {
            Service.Resolve<IEventLogService>().LogError(nameof(AlgoliaConnection), code, message);
        }
    }
}