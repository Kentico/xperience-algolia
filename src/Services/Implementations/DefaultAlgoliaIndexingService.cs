using CMS;
using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.FormEngine;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Services;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: RegisterImplementation(typeof(IAlgoliaIndexingService), typeof(DefaultAlgoliaIndexingService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaIndexingService"/>.
    /// </summary>
    internal class DefaultAlgoliaIndexingService : IAlgoliaIndexingService
    {
        private readonly IAlgoliaConnection algoliaConnection;
        private readonly IAlgoliaRegistrationService algoliaRegistrationService;
        private readonly IEventLogService eventLogService;


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAlgoliaInsightsService"/> class.
        /// </summary>
        public DefaultAlgoliaIndexingService(IAlgoliaRegistrationService algoliaRegistrationService,
            IAlgoliaConnection algoliaConnection,
            IEventLogService eventLogService)
        {
            this.algoliaRegistrationService = algoliaRegistrationService;
            this.algoliaConnection = algoliaConnection;
            this.eventLogService = eventLogService;
        }


        public void EnqueueAlgoliaItems(TreeNode node, string eventName)
        {
            foreach (var index in algoliaRegistrationService.RegisteredIndexes)
            {
                if (!algoliaRegistrationService.IsNodeIndexedByIndex(node, index.IndexName))
                {
                    continue;
                }

                var indexedColumns = algoliaRegistrationService.GetIndexedColumnNames(index.IndexName);
                if (indexedColumns.Length == 0)
                {
                    eventLogService.LogError(nameof(DefaultAlgoliaIndexingService), nameof(EnqueueAlgoliaItems), $"Unable to enqueue node change: Error loading indexed columns.");
                    continue;
                }

                if (node.GetWorkflow() == null && eventName.Equals(DocumentEvents.Update.Name) && !node.AnyItemChanged(indexedColumns))
                {
                    // For updated non-workflow pages, don't update Algolia if nothing changed
                    continue;
                }

                var shouldDelete = eventName.Equals(DocumentEvents.Delete.Name, StringComparison.OrdinalIgnoreCase) ||
                    eventName.Equals(WorkflowEvents.Archive.Name, StringComparison.OrdinalIgnoreCase);
                AlgoliaQueueWorker.EnqueueAlgoliaQueueItem(new AlgoliaQueueItem
                {
                    Node = node,
                    Delete = shouldDelete,
                    IndexName = index.IndexName
                });
            }
        }


        public IEnumerable<JObject> GetTreeNodeData(TreeNode node, AlgoliaIndex algoliaIndex)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (algoliaIndex == null)
            {
                throw new ArgumentNullException(nameof(algoliaIndex));
            }

            var data = new JObject();
            MapTreeNodeProperties(node, data, algoliaIndex.Type);
            MapCommonProperties(node, data);

            if (algoliaIndex.DistinctOptions != null)
            {
                var searchModel = Activator.CreateInstance(algoliaIndex.Type) as AlgoliaSearchModel;
                return searchModel.SplitData(data);
            }

            return new JObject[] { data };
        }


        public int ProcessAlgoliaTasks(IEnumerable<AlgoliaQueueItem> items)
        {
            var successfulOperations = 0;

            // Group queue items based on index name
            var groups = items.ToList().GroupBy(item => item.IndexName);
            foreach (var group in groups)
            {
                try
                {
                    algoliaConnection.Initialize(group.Key);

                    var algoliaIndex = algoliaRegistrationService.RegisteredIndexes.FirstOrDefault(i => i.IndexName == group.Key);
                    if (algoliaIndex == null)
                    {
                        eventLogService.LogError(nameof(DefaultAlgoliaIndexingService), nameof(ProcessAlgoliaTasks), $"Attempted to process tasks for index '{group.Key},' but the index is not registered.");
                        continue;
                    }

                    var deleteTasks = group.Where(queueItem => queueItem.Delete);
                    var updateTasks = group.Where(queueItem => !queueItem.Delete);

                    var deleteIds = new List<string>();
                    if (algoliaIndex.DistinctOptions != null)
                    {
                        // Data has been split, call GetTreeNodeData to obtain IDs of the smaller records
                        foreach (var queueItem in deleteTasks)
                        {
                            var splitData = GetTreeNodeData(queueItem.Node, algoliaIndex);
                            deleteIds.AddRange(splitData.Select(obj => obj.Value<string>("objectID")));
                        }
                    }
                    else
                    {
                        deleteIds.AddRange(deleteTasks.Select(queueItem => queueItem.Node.DocumentID.ToString()));
                    }

                    var upsertData = new List<JObject>();
                    foreach (var queueItem in updateTasks)
                    {
                        upsertData.AddRange(GetTreeNodeData(queueItem.Node, algoliaIndex));
                    }

                    successfulOperations += algoliaConnection.UpsertRecords(upsertData);
                    successfulOperations += algoliaConnection.DeleteRecords(deleteIds);
                }
                catch (InvalidOperationException ex)
                {
                    eventLogService.LogError(nameof(DefaultAlgoliaIndexingService), nameof(ProcessAlgoliaTasks), ex.Message);
                }
                catch (ArgumentNullException ex)
                {
                    eventLogService.LogError(nameof(DefaultAlgoliaIndexingService), nameof(ProcessAlgoliaTasks), ex.Message);
                }
            }

            return successfulOperations;
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
                    eventLogService.LogError(nameof(DefaultAlgoliaIndexingService), nameof(GetAbsoluteUrlForColumn), $"Unable to load field definition for page type '{node.ClassName}' column name '{columnName}.'");
                    return null;
                }

                switch (field.DataType)
                {
                    case FieldDataType.File: // Attachment
                        var attachment = AttachmentInfo.Provider.Get(new Guid(strValue), node.NodeSiteID);
                        if (attachment == null)
                        {
                            return null;
                        }

                        nodeValue = AttachmentURLProvider.GetAttachmentUrl(attachment.AttachmentGUID, attachment.AttachmentName);
                        break;
                }
            }

            var liveSiteDomain = node.Site.SitePresentationURL;
            return URLHelper.GetAbsoluteUrl(ValidationHelper.GetString(nodeValue, ""), null, liveSiteDomain, null);
        }


        /// <summary>
        /// Gets the <paramref name="node"/> value using the <paramref name="property"/>
        /// name, or the property's <see cref="SourceAttribute"/> if specified.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load a value from.</param>
        /// <param name="property">The Algolia search model property.</param>
        /// <param name="searchModelType">The Algolia search model.</param>
        protected object GetNodeValue(TreeNode node, PropertyInfo property, Type searchModelType)
        {
            object nodeValue = null;
            string usedColumn = null;
            var searchModel = Activator.CreateInstance(searchModelType) as AlgoliaSearchModel;

            if (!Attribute.IsDefined(property, typeof(SourceAttribute)))
            {
                nodeValue = node.GetValue(property.Name);
                return searchModel.OnIndexingProperty(node, property.Name, usedColumn, nodeValue);
            }

            // Property uses SourceAttribute, loop through column names until a non-null value is found
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

            // Convert node value to URL by referencing the used source column
            if (Attribute.IsDefined(property, typeof(UrlAttribute)))
            {
                nodeValue = GetAbsoluteUrlForColumn(node, nodeValue, usedColumn);
            }

            nodeValue = searchModel.OnIndexingProperty(node, property.Name, usedColumn, nodeValue);

            return nodeValue;
        }


        /// <summary>
        /// Locates the registered search model properties which match the property names of the passed
        /// <paramref name="node"/> and sets the <paramref name="data"/> values from the <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load values from.</param>
        /// <param name="data">The dynamic data that will be passed to Algolia.</param>
        /// <param name="searchModelType">The class of the Algolia search model.</param>
        protected void MapTreeNodeProperties(TreeNode node, JObject data, Type searchModelType)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new DecimalPrecisionConverter());

            var searchModel = Activator.CreateInstance(searchModelType);
            PropertyInfo[] properties = searchModel.GetType().GetProperties();
            foreach (var prop in properties)
            {
                if (prop.DeclaringType == typeof(AlgoliaSearchModel))
                {
                    continue;
                }

                object nodeValue = GetNodeValue(node, prop, searchModelType);
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
        protected void MapCommonProperties(TreeNode node, JObject data)
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
