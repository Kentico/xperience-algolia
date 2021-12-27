using Algolia.Search.Clients;
using Algolia.Search.Models.Common;
using Algolia.Search.Models.Settings;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.FormEngine;
using CMS.Helpers;
using Kentico.Xperience.AlgoliaSearch.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
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
        public AlgoliaConnection(string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                //TODO: Throw exception or log message
                return;
            }

            var indexSettings = AlgoliaSearchHelper.GetIndexSettings(indexName);
            searchIndex = AlgoliaSearchHelper.GetSearchIndex(indexName);
            searchModelType = AlgoliaSearchHelper.GetModelByIndexName(indexName);
            searchIndex.SetSettings(indexSettings);
        }


        /// <summary>
        /// Updates the Algolia index with the <paramref name="node"/> property values that are
        /// specified in the registered search model class. The internal Algolia object ID is set to
        /// the passed <paramref name="node"/>'s <see cref="TreeNode.DocumentID"/>.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load property values from.</param>
        /// <returns>The response of the request from Algolia.</returns>
        public BatchIndexingResponse UpsertTreeNode(TreeNode node)
        {
            //TODO: Validate node
            var data = new JObject();
            MapTreeNodeProperties(node, data);

            return searchIndex.SaveObject(data);
        }


        /// <summary>
        /// Rebuilds the Algolia index by removing existing data from Algolia and indexing all
        /// pages in the content tree included in the index.
        /// </summary>
        public void Rebuild()
        {
            searchIndex.ClearObjects();

            //TODO: Validate searchModelType
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

            //TODO: Add batching for upserts
            foreach (var node in indexedNodes)
            {
                UpsertTreeNode(node);
            }
        }


        /// <summary>
        /// Locates the registered search model properties which match the property names of the passed
        /// <paramref name="node"/> and sets the <paramref name="data"/> values from the <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load values from.</param>
        /// <param name="data">The dynamic data that will be passed to Algolia.</param>
        private void MapTreeNodeProperties(TreeNode node, JObject data)
        {
            //TODO: Validate searchModelType, node, data
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new DecimalPrecisionConverter());

            var searchModel = Activator.CreateInstance(searchModelType);
            PropertyInfo[] properties = searchModel.GetType().GetProperties();
            foreach (var prop in properties)
            {
                object nodeValue = null;
                if (Attribute.IsDefined(prop, typeof(UrlAttribute)))
                {
                    nodeValue = GetAbsoluteUrlForProperty(node, prop);
                }
                else
                {
                    nodeValue = node.GetValue(prop.Name);
                }
                
                if(nodeValue == null)
                {
                    continue;
                }
                
                var convertedName = AlgoliaSearchHelper.ConvertToCamelCase(prop.Name);
                data.Add(convertedName, JToken.FromObject(nodeValue, serializer));
                
            }

            data["objectID"] = node.DocumentID.ToString();
        }


        private string GetAbsoluteUrlForProperty(TreeNode node, PropertyInfo property)
        {
            var nodeValue = ValidationHelper.GetString(node.GetValue(property.Name), "");

            if (String.IsNullOrEmpty(nodeValue))
            {
                return null;
            }

            if (!nodeValue.StartsWith("~"))
            {
                // Value is not a URL, get field data type and load URL
                var dataClassInfo = DataClassInfoProvider.GetDataClassInfo(node.ClassName, false);
                var formInfo = new FormInfo(dataClassInfo.ClassFormDefinition);
                var field = formInfo.GetFormField(property.Name);

                if (field == null)
                {
                    // TODO: Throw or log error
                    return null;
                }

                switch (field.DataType)
                {
                    case FieldDataType.File: // Attachment
                        var attachment = AttachmentInfo.Provider.Get(new Guid(nodeValue), node.NodeSiteID);
                        nodeValue = AttachmentURLProvider.GetAttachmentUrl(attachment.AttachmentGUID, attachment.AttachmentName);
                        break;
                }
            }

            var liveSiteDomain = node.Site.SitePresentationURL;
            return URLHelper.GetAbsoluteUrl(ValidationHelper.GetString(nodeValue, ""), null, liveSiteDomain, null);
        }
    }
}