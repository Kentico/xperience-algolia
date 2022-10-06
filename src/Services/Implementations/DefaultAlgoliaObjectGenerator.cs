using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.FormEngine;
using CMS.Helpers;

using Kentico.Xperience.Algolia.Attributes;
using Kentico.Xperience.Algolia.Models;
using Kentico.Xperience.AlgoliaSearch;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.Algolia.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaObjectGenerator"/>.
    /// </summary>
    internal class DefaultAlgoliaObjectGenerator : IAlgoliaObjectGenerator
    {
        private readonly IConversionService conversionService;
        private readonly IEventLogService eventLogService;
        private readonly Dictionary<string, string[]> cachedIndexedColumns = new();
        private readonly string[] ignoredPropertiesForTrackingChanges = new string[] {
            nameof(AlgoliaSearchModel.ObjectID),
            nameof(AlgoliaSearchModel.Url),
            nameof(AlgoliaSearchModel.ClassName)
        };


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAlgoliaObjectGenerator"/> class.
        /// </summary>
        public DefaultAlgoliaObjectGenerator(IConversionService conversionService, IEventLogService eventLogService)
        {
            this.conversionService = conversionService;
            this.eventLogService = eventLogService;
        }


        /// <inheritdoc/>
        public JObject GetTreeNodeData(AlgoliaQueueItem queueItem)
        {
            var data = new JObject();
            MapChangedProperties(queueItem, data);
            MapCommonProperties(queueItem.Node, data);

            return data;
        }


        /// <inheritdoc/>
        public IEnumerable<JObject> SplitData(JObject originalData, AlgoliaIndex algoliaIndex)
        {
            // No data splitting by default
            return new JObject[] { originalData };
        }


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
        protected string GetAbsoluteUrlForColumn(TreeNode node, object nodeValue, string columnName)
        {
            var strValue = conversionService.GetString(nodeValue, String.Empty);
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
                    eventLogService.LogError(nameof(DefaultAlgoliaObjectGenerator), nameof(GetAbsoluteUrlForColumn), $"Unable to load field definition for page type '{node.ClassName}' column name '{columnName}.'");
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
            return URLHelper.GetAbsoluteUrl(conversionService.GetString(nodeValue, String.Empty), null, liveSiteDomain, null);
        }


        /// <summary>
        /// Gets the names of all database columns which are indexed by the passed index,
        /// minus those listed in <see cref="ignoredPropertiesForTrackingChanges"/>.
        /// </summary>
        /// <param name="indexName">The index to load columns for.</param>
        /// <returns>The database columns that are indexed.</returns>
        private string[] GetIndexedColumnNames(string indexName)
        {
            if (cachedIndexedColumns.TryGetValue(indexName, out string[] value))
            {
                return value;
            }

            // Don't include properties with SourceAttribute at first, check the sources and add to list after
            var algoliaIndex = IndexStore.Instance.Get(indexName);
            var indexedColumnNames = algoliaIndex.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => !Attribute.IsDefined(prop, typeof(SourceAttribute))).Select(prop => prop.Name).ToList();
            var propertiesWithSourceAttribute = algoliaIndex.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(prop => Attribute.IsDefined(prop, typeof(SourceAttribute)));
            foreach (var property in propertiesWithSourceAttribute)
            {
                var sourceAttribute = property.GetCustomAttributes<SourceAttribute>(false).FirstOrDefault();
                if (sourceAttribute == null)
                {
                    continue;
                }

                indexedColumnNames.AddRange(sourceAttribute.Sources);
            }

            // Remove column names from AlgoliaSearchModel that aren't database columns
            indexedColumnNames.RemoveAll(col => ignoredPropertiesForTrackingChanges.Contains(col));

            var indexedColumns = indexedColumnNames.ToArray();
            cachedIndexedColumns.Add(indexName, indexedColumns);

            return indexedColumns;
        }


        /// <summary>
        /// Gets the <paramref name="node"/> value using the <paramref name="property"/>
        /// name, or the property's <see cref="SourceAttribute"/> if specified.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load a value from.</param>
        /// <param name="property">The Algolia search model property.</param>
        /// <param name="searchModelType">The Algolia search model.</param>
        /// <param name="columnsToUpdate">A list of columns to retrieve values for. Columns not present
        /// in this list will return <c>null</c>.</param>
        private object GetNodeValue(TreeNode node, PropertyInfo property, Type searchModelType, IEnumerable<string> columnsToUpdate)
        {
            object nodeValue = null;
            var usedColumn = property.Name;
            if (Attribute.IsDefined(property, typeof(SourceAttribute)))
            {
                // Property uses SourceAttribute, loop through column names until a non-null value is found
                var sourceAttribute = property.GetCustomAttributes<SourceAttribute>(false).FirstOrDefault();
                foreach (var source in sourceAttribute.Sources.Where(s => columnsToUpdate.Contains(s)))
                {
                    nodeValue = node.GetValue(source);
                    if (nodeValue != null)
                    {
                        usedColumn = source;
                        break;
                    }
                }
            }
            else
            {
                if (!columnsToUpdate.Contains(property.Name))
                {
                    return null;
                }

                nodeValue = node.GetValue(property.Name);
            }

            // Convert node value to URL by referencing the used source column
            if (Attribute.IsDefined(property, typeof(UrlAttribute)))
            {
                nodeValue = GetAbsoluteUrlForColumn(node, nodeValue, usedColumn);
            }

            var searchModel = Activator.CreateInstance(searchModelType) as AlgoliaSearchModel;
            nodeValue = searchModel.OnIndexingProperty(node, property.Name, usedColumn, nodeValue);

            return nodeValue;
        }


        /// <summary>
        /// Adds values to the <paramref name="data"/> by retriving the indexed columns of the index
        /// and getting values from the <see cref="AlgoliaQueueItem.Node"/>. When the <see cref="AlgoliaQueueItem.TaskType"/>
        /// is <see cref="AlgoliaTaskType.UPDATE"/>, only the <see cref="AlgoliaQueueItem.ChangedColumns"/>
        /// will be added to the <paramref name="data"/>.
        /// </summary>
        private void MapChangedProperties(AlgoliaQueueItem queueItem, JObject data)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new DecimalPrecisionConverter());

            var columnsToUpdate = new List<string>();
            var indexedColumns = GetIndexedColumnNames(queueItem.IndexName);
            if (queueItem.TaskType == AlgoliaTaskType.CREATE)
            {
                columnsToUpdate.AddRange(indexedColumns);
            }
            else if (queueItem.TaskType == AlgoliaTaskType.UPDATE)
            {
                columnsToUpdate.AddRange(queueItem.ChangedColumns.Intersect(indexedColumns));
            }

            var algoliaIndex = IndexStore.Instance.Get(queueItem.IndexName);
            var properties = algoliaIndex.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                object nodeValue = GetNodeValue(queueItem.Node, prop, algoliaIndex.Type, columnsToUpdate);
                if (nodeValue == null)
                {
                    continue;
                }

                data.Add(prop.Name, JToken.FromObject(nodeValue, serializer));
            }
        }


        /// <summary>
        /// Sets values in the <paramref name="data"/> object using the common search model properties
        /// located within the <see cref="AlgoliaSearchModel"/> class.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load values from.</param>
        /// <param name="data">The dynamic data that will be passed to Algolia.</param>
        private void MapCommonProperties(TreeNode node, JObject data)
        {
            data["objectID"] = node.DocumentID.ToString();
            data[nameof(AlgoliaSearchModel.ClassName)] = node.ClassName;

            try
            {
                data[nameof(AlgoliaSearchModel.Url)] = DocumentURLProvider.GetAbsoluteUrl(node);
            }
            catch (Exception)
            {
                // GetAbsoluteUrl can throw an exception when processing a page update AlgoliaQueueItem
                // and the page was deleted before the update task has processed. In this case, upsert an
                // empty URL
                data[nameof(AlgoliaSearchModel.Url)] = String.Empty;
            }

            // Convert scheduled publishing times to Unix timestamp in UTC
            var publishToUnix = Int32.MaxValue;
            if (node.DocumentPublishTo != DateTime.MaxValue)
            {
                var nodePublishToUnix = node.DocumentPublishTo.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                publishToUnix = ValidationHelper.GetInteger(nodePublishToUnix, publishToUnix);
            }
            var publishFromUnix = 0;
            if (node.DocumentPublishFrom != DateTime.MinValue)
            {
                var nodePublishFromUnix = node.DocumentPublishFrom.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                publishFromUnix = ValidationHelper.GetInteger(nodePublishFromUnix, publishFromUnix);
            }

            data[nameof(AlgoliaSearchModel.DocumentPublishTo)] = publishToUnix;
            data[nameof(AlgoliaSearchModel.DocumentPublishFrom)] = publishFromUnix;
        }
    }
}
