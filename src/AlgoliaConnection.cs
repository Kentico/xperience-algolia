using Algolia.Search.Clients;
using Algolia.Search.Models.Settings;

using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.FormEngine;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Helpers;
using Kentico.Xperience.AlgoliaSearch.Models;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
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
        /// Constructor. Initializes the Algolia index and its <see cref="IndexSettings"/> by scanning
        /// the registered search model class for custom attributes.
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

            var indexSettings = AlgoliaSearchHelper.GetIndexSettings(indexName);
            if (indexSettings == null)
            {
                throw new InvalidOperationException("Unable to load search index settings.");
            }

            var configuration = Service.ResolveOptional<IConfiguration>();
            var client = AlgoliaSearchHelper.GetSearchClient(configuration);

            searchIndex = client.InitIndex(indexName);
            searchModelType = AlgoliaSearchHelper.GetModelByIndexName(indexName);
            if (searchModelType.BaseType != typeof(AlgoliaSearchModel))
            {
                throw new InvalidOperationException("Algolia search models must extend the AlgoliaSearchModel class.");
            }

            searchIndex.SetSettings(indexSettings);
        }


        /// <summary>
        /// Removes records from the Algolia index. The <see cref="TreeNode.DocumentID"/> of
        /// each <see cref="TreeNode"/> is used to reference the internal Algolia object ID
        /// to delete.
        /// </summary>
        /// <param name="nodes">The <see cref="TreeNode"/>s to delete.</param>
        public void DeleteTreeNodes(IEnumerable<TreeNode> nodes)
        {
            if (nodes == null || nodes.Count() == 0)
            {
                return;
            }

            var documentIds = nodes.Select(node => node.DocumentID.ToString());
            searchIndex.DeleteObjects(documentIds);
        }


        /// <summary>
        /// Updates the Algolia index with the property values that are specified in the
        /// registered search model class for each <see cref="TreeNode"/> in the <paramref name="nodes"/>
        /// collection. The internal Algolia object IDs are set to the <see cref="TreeNode.DocumentID"/>
        /// of each <see cref="TreeNode"/>.
        /// </summary>
        /// <param name="nodes">The <see cref="TreeNode"/>s to load property values from.</param>
        public void UpsertTreeNodes(IEnumerable<TreeNode> nodes)
        {
            if (nodes == null || nodes.Count() == 0)
            {
                return;
            }

            try
            {
                var dataObjects = new List<JObject>();
                foreach (var node in nodes)
                {
                    var data = new JObject();
                    MapTreeNodeProperties(node, data);
                    dataObjects.Add(data);
                }
                
                searchIndex.SaveObjects(dataObjects);
            }
            catch (InvalidOperationException ex)
            {
                LogError(nameof(UpsertTreeNodes), ex.Message);
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


        /// <summary>
        /// Gets the <paramref name="node"/> value using the <paramref name="property"/>
        /// name, or the property's <see cref="SourceAttribute"/> if specified.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load a value from.</param>
        /// <param name="property">The Algolia search model property.</param>
        /// <returns></returns>
        private object GetNodeValue(TreeNode node, PropertyInfo property)
        {
            if (!Attribute.IsDefined(property, typeof(SourceAttribute)))
            {
                return node.GetValue(property.Name);
            }

            // Property uses SourceAttribute, loop through column names until a non-null value is found
            object nodeValue = null;
            string usedColumn = null;
            var sourceAttribute = property.GetCustomAttributes<SourceAttribute>(false).FirstOrDefault();
            foreach (var source in sourceAttribute.Sources)
            {
                nodeValue = node.GetValue(source);
                if (nodeValue != null)
                {
                    usedColumn = source;
                    break;
                }
            }

            if (nodeValue == null)
            {
                return null;
            }

            // Convert node value to URL by referencing the used source column
            if (Attribute.IsDefined(property, typeof(UrlAttribute)))
            {
                nodeValue = GetAbsoluteUrlForColumn(node, nodeValue, usedColumn);
            }

            return nodeValue;
        }


        /// <summary>
        /// Locates the registered search model properties which match the property names of the passed
        /// <paramref name="node"/> and sets the <paramref name="data"/> values from the <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load values from.</param>
        /// <param name="data">The dynamic data that will be passed to Algolia.</param>
        /// <exception cref="InvalidOperationException">Thrown if a search model class is not
        /// found for the index.</exception>
        private void MapTreeNodeProperties(TreeNode node, JObject data)
        {
            if (searchModelType == null)
            {
                throw new InvalidOperationException("No registered search model class found for index.");
            }

            var serializer = new JsonSerializer();
            serializer.Converters.Add(new DecimalPrecisionConverter());

            var searchModel = Activator.CreateInstance(searchModelType);
            PropertyInfo[] properties = searchModel.GetType().GetProperties();
            foreach (var prop in properties)
            {
                object nodeValue = GetNodeValue(node, prop);
                if(nodeValue == null)
                {
                    continue;
                }

                var convertedName = AlgoliaSearchHelper.ConvertToCamelCase(prop.Name);
                data.Add(convertedName, JToken.FromObject(nodeValue, serializer));
                
            }
            try
            {
                data["url"] = DocumentURLProvider.GetAbsoluteUrl(node);
            }
            catch (Exception ex)
            {
                // GetAbsoluteUrl can throw an exception when processing a page update AlgoliaQueueItem
                // and the page was deleted before the update task has processed. In this case, upsert an
                // empty URL
                data["url"] = String.Empty;
            }

            data["objectID"] = node.DocumentID.ToString();
        }


        /// <summary>
        /// Converts the value from the <paramref name="node"/>'s column from a relative URL
        /// (e.g. ~/getmedia) or an attachment reference into an absolute live-site URL.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> the value was loaded from.</param>
        /// <param name="nodeValue">The original value of the column.</param>
        /// <param name="columnName">The name of the column the value was loaded from.</param>
        /// <returns>An absolute URL, or null if it couldn't be converted.</returns>
        private string GetAbsoluteUrlForColumn(TreeNode node, object nodeValue, string columnName)
        {
            var strValue = ValidationHelper.GetString(nodeValue, "");
            if (String.IsNullOrEmpty(strValue))
            {
                return null;
            }

            if (!strValue.StartsWith("~"))
            {
                // Value is not a URL, get field data type and load URL
                var dataClassInfo = DataClassInfoProvider.GetDataClassInfo(node.ClassName, false);
                var formInfo = new FormInfo(dataClassInfo.ClassFormDefinition);
                var field = formInfo.GetFormField(columnName);

                if (field == null)
                {
                    LogError(nameof(GetAbsoluteUrlForColumn), $"Unable to load field definition for page type '{node.ClassName}' column name '{columnName}.'");
                    return null;
                }

                switch (field.DataType)
                {
                    case FieldDataType.File: // Attachment
                        var attachment = AttachmentInfo.Provider.Get(new Guid(strValue), node.NodeSiteID);
                        nodeValue = AttachmentURLProvider.GetAttachmentUrl(attachment.AttachmentGUID, attachment.AttachmentName);
                        break;
                }
            }

            var liveSiteDomain = node.Site.SitePresentationURL;
            return URLHelper.GetAbsoluteUrl(ValidationHelper.GetString(nodeValue, ""), null, liveSiteDomain, null);
        }


        private void LogError(string code, string message)
        {
            Service.Resolve<IEventLogService>().LogError(nameof(AlgoliaConnection), code, message);
        }
    }
}