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

[assembly: RegisterImplementation(typeof(AlgoliaIndexingService), typeof(DefaultAlgoliaIndexingService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Default implementation of <see cref="AlgoliaIndexingService"/>.
    /// </summary>
    public class DefaultAlgoliaIndexingService : AlgoliaIndexingService
    {
        private readonly AlgoliaConnection algoliaConnection;
        private readonly AlgoliaRegistrationService algoliaRegistrationService;
        private readonly IEventLogService eventLogService;


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAlgoliaInsightsService"/> class.
        /// </summary>
        public DefaultAlgoliaIndexingService(AlgoliaRegistrationService algoliaRegistrationService,
            AlgoliaConnection algoliaConnection,
            IEventLogService eventLogService)
        {
            this.algoliaRegistrationService = algoliaRegistrationService;
            this.algoliaConnection = algoliaConnection;
            this.eventLogService = eventLogService;
        }


        public override void EnqueueAlgoliaItems(TreeNode node, bool wasDeleted, bool isNew)
        {
            foreach (var index in algoliaRegistrationService.RegisteredIndexes)
            {
                if (!algoliaRegistrationService.IsNodeIndexedByIndex(node, index.Key))
                {
                    continue;
                }

                var indexedColumns = algoliaRegistrationService.GetIndexedColumnNames(index.Key);
                if (indexedColumns.Length == 0)
                {
                    eventLogService.LogError(nameof(DefaultAlgoliaIndexingService), nameof(EnqueueAlgoliaItems), $"Unable to enqueue node change: Error loading indexed columns.");
                    continue;
                }

                if (!isNew && !wasDeleted && !node.AnyItemChanged(indexedColumns))
                {
                    continue;
                }

                AlgoliaQueueWorker.EnqueueAlgoliaQueueItem(new AlgoliaQueueItem()
                {
                    Node = node,
                    Deleted = wasDeleted,
                    IndexName = index.Key
                });
            }
        }


        public override JObject GetTreeNodeData(TreeNode node, Type searchModelType)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (searchModelType == null)
            {
                throw new ArgumentNullException(nameof(searchModelType));
            }

            var data = new JObject();
            MapTreeNodeProperties(node, data, searchModelType);
            MapCommonProperties(node, data);

            return data;
        }


        public override int ProcessAlgoliaTasks(IEnumerable<AlgoliaQueueItem> items)
        {
            var successfulOperations = 0;

            // Group queue items based on index name
            var groups = items.ToList().GroupBy(item => item.IndexName);
            foreach (var group in groups)
            {
                try
                {
                    algoliaConnection.Initialize(group.Key);

                    var searchModelType = algoliaRegistrationService.GetModelByIndexName(group.Key);
                    var deleteTasks = group.Where(queueItem => queueItem.Deleted);
                    var updateTasks = group.Where(queueItem => !queueItem.Deleted);
                    var upsertData = updateTasks.Select(queueItem => GetTreeNodeData(queueItem.Node, searchModelType));
                    var deleteData = deleteTasks.Select(queueItem => queueItem.Node.DocumentID.ToString());

                    successfulOperations += algoliaConnection.UpsertRecords(upsertData);
                    successfulOperations += algoliaConnection.DeleteRecords(deleteData);
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


        protected override string GetAbsoluteUrlForColumn(TreeNode node, object nodeValue, string columnName)
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
                        nodeValue = AttachmentURLProvider.GetAttachmentUrl(attachment.AttachmentGUID, attachment.AttachmentName);
                        break;
                }
            }

            var liveSiteDomain = node.Site.SitePresentationURL;
            return URLHelper.GetAbsoluteUrl(ValidationHelper.GetString(nodeValue, ""), null, liveSiteDomain, null);
        }


        protected override object GetNodeValue(TreeNode node, PropertyInfo property, Type searchModelType)
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


        protected override void MapTreeNodeProperties(TreeNode node, JObject data, Type searchModelType)
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


        protected override void MapCommonProperties(TreeNode node, JObject data)
        {
            data["objectID"] = node.DocumentID.ToString();
            data[nameof(AlgoliaSearchModel.ClassName)] = node.ClassName;

            try
            {
                data[nameof(AlgoliaSearchModel.Url)] = DocumentURLProvider.GetAbsoluteUrl(node);
            }
            catch (Exception ex)
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
