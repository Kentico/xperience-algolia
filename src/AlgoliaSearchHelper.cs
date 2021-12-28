using Algolia.Search.Clients;
using Algolia.Search.Models.Common;
using Algolia.Search.Models.Settings;

using CMS.Base;
using CMS.Core;
using CMS.DocumentEngine;
using CMS.Helpers;

using Kentico.Xperience.Algolia.Models;
using Kentico.Xperience.AlgoliaSearch.Attributes;

using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Contains methods for common Algolia tasks and stores all registered Algolia indexes.
    /// </summary>
    public class AlgoliaSearchHelper
    {
        private static Dictionary<string, Type> mRegisteredIndexes = new Dictionary<string, Type>();


        /// <summary>
        /// A collection of Algolia index names and the object type which represents the columns
        /// included in the index.
        /// </summary>
        public static Dictionary<string, Type> RegisteredIndexes
        {
            get
            {
                return mRegisteredIndexes;
            }
        }


        /// <summary>
        /// Gets the registered search model class that is paired with the Algolia index.
        /// </summary>
        /// <param name="indexName">The code name of the Algolia index.</param>
        /// <returns>The search model class type, or null if not found.</returns>
        public static Type GetModelByIndexName(string indexName)
        {
            //TODO: Validate indexName
            var records = mRegisteredIndexes.Where(i => i.Key == indexName);
            if (records.Count() == 0)
            {
                return null;
            }

            return records.FirstOrDefault().Value;
        }


        /// <summary>
        /// Gets a <see cref="SearchClient"/> using the application ID and API key specified in
        /// either the web.config or appsettings.json, depending on the application.
        /// </summary>
        /// <returns>A <see cref="SearchClient"/> for interfacing with Algolia.</returns>
        public static SearchClient GetSearchClient()
        {
            AlgoliaOptions options = null;
            if (SystemContext.IsCMSRunningAsMainApplication)
            {
                options = GetAlgoliaOptionsFramework();
            }
            else
            {
                options = GetAlgoliaOptionsCore();
            }

            //TODO: Validate options
            return new SearchClient(options.ApplicationId, options.ApiKey);
        }


        /// <summary>
        /// Gets an instance of <see cref="SearchIndex"/> for the specified Algolia index.
        /// </summary>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <returns>A <see cref="SearchIndex"/> to search with, or null if not found.</returns>
        public static SearchIndex GetSearchIndex(string indexName)
        {
            //TODO: Validate indexName
            var client = GetSearchClient();

            //TODO: Validate client
            return client.InitIndex(indexName);
        }


        /// <summary>
        /// Gets the indices of the Algolia application with basic statistics.
        /// </summary>
        /// <remarks>See <see href="https://www.algolia.com/doc/api-reference/api-methods/list-indices/#response"/></remarks>
        public static List<IndicesResponse> GetStatistics()
        {
            var client = GetSearchClient();

            //TODO: Validate client
            return client.ListIndices().Items;
        }


        /// <summary>
        /// Gets the <see cref="IndexSettings"/> of the Algolia index.
        /// </summary>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <returns>The index settings.</returns>
        public static IndexSettings GetIndexSettings(string indexName)
        {
            //TODO: Validate indexName
            var searchModelType = GetModelByIndexName(indexName);
            if (searchModelType == null)
            {
                //TODO: Throw or log error
                return null;
            }

            var searchableProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(SearchableAttribute)));
            var retrievablProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(RetrievableAttribute)));
            var facetableProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(FacetableAttribute)));
            ;
            return new IndexSettings()
            {
                SearchableAttributes = OrderSearchableProperties(searchableProperties),
                AttributesToRetrieve = retrievablProperties.Select(p => ConvertToCamelCase(p.Name)).ToList(),
                AttributesForFaceting = facetableProperties.Select(GetFilterablePropertyName).ToList()
            };
        }


        private static string GetFilterablePropertyName(PropertyInfo property)
        {
            var attr = property.GetCustomAttributes<FacetableAttribute>(false).FirstOrDefault();
            if (attr.FilterOnly && attr.Searchable)
            {
                throw new InvalidOperationException("Facetable attributes cannot be both searchable and filterOnly.");
            }

            var name = ConvertToCamelCase(property.Name);
            if (attr.FilterOnly)
            {
                return $"filterOnly({name})";
            }
            if (attr.Searchable)
            {
                return $"searchable({name})";
            }

            return name;
        }


        private static List<string> OrderSearchableProperties(IEnumerable<PropertyInfo> searchableProperties)
        {
            var propertiesWithAttribute = new Dictionary<string, SearchableAttribute>();
            foreach (var prop in searchableProperties)
            {
                var attr = prop.GetCustomAttributes<SearchableAttribute>(false).FirstOrDefault();
                propertiesWithAttribute.Add(prop.Name, attr);
            }

            // Remove properties without order, add to end of list later
            var propertiesWithOrdering = propertiesWithAttribute.Where(prop => prop.Value.Order >= 0);
            var sortedByOrder = propertiesWithOrdering.OrderBy(prop => prop.Value.Order);
            var groupedByOrder = sortedByOrder.GroupBy(prop => prop.Value.Order);
            var searchableAttributes = groupedByOrder.Select(group =>
                group.Select(prop =>
                {
                    var propName = ConvertToCamelCase(prop.Key);
                    if (prop.Value.Unordered)
                    {
                        return $"unordered({propName})";
                    }

                    return propName;
                }).Join(",")
            ).ToList();

            // Add properties without order as single items
            var propertiesWithoutOrdering = propertiesWithAttribute.Where(prop => prop.Value.Order == -1);
            foreach (var prop in propertiesWithoutOrdering)
            {
                var propName = ConvertToCamelCase(prop.Key);
                if (prop.Value.Unordered)
                {
                    searchableAttributes.Add($"unordered({propName})");
                    continue;
                }

                searchableAttributes.Add(propName);
            }

            return searchableAttributes;
        }


        /// <summary>
        /// Returns true if the passed node's <see cref="TreeNode.NodeAliasPath"/> is included in an
        /// Algolia index's allowed paths, and the node's <see cref="TreeNode.ClassName"/> is included
        /// in a matching allowed path.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsNodeAlgoliaIndexed(TreeNode node)
        {
            if (node == null)
            {
                //TODO: Throw or log error
                return false;
            }

            foreach (var index in mRegisteredIndexes)
            {
                if (IsNodeIndexedByIndex(node, index.Key))
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Returns true if the <paramref name="node"/> is included in the Algolia index's allowed
        /// paths as set by the <see cref="IncludedPathAttribute"/>.
        /// </summary>
        /// <param name="node">The node to check for indexing.</param>
        /// <param name="indexName">The Algolia index code name.</param>
        public static bool IsNodeIndexedByIndex(TreeNode node, string indexName)
        {
            //TODO: Validate indexName
            if (node == null)
            {
                //TODO: Throw or log error
                return false;
            }

            var searchModelType = GetModelByIndexName(indexName);
            if (searchModelType == null)
            {
                //TODO: Throw or log error
            }

            var includedPathAttributes = searchModelType.GetCustomAttributes<IncludedPathAttribute>(false);
            foreach (var includedPathAttribute in includedPathAttributes)
            {
                var path = includedPathAttribute.AliasPath;
                var matchesPageType = (includedPathAttribute.PageTypes.Length == 0 || includedPathAttribute.PageTypes.Contains(node.ClassName));
                var matchesCulture = (includedPathAttribute.Cultures.Length == 0 || includedPathAttribute.Cultures.Contains(node.DocumentCulture));

                if (path.EndsWith("/%"))
                {
                    path = path.TrimEnd('%', '/');
                    if (node.NodeAliasPath.StartsWith(path) && matchesPageType && matchesCulture)
                    {
                        return true;
                    }
                }
                else
                {
                    if (node.NodeAliasPath == path && matchesPageType && matchesCulture)
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Saves an Algolia index code name and its search model to the <see cref="RegisteredIndexes"/>.
        /// </summary>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <param name="searchModelType">The search model type.</param>
        public static void RegisterIndex(string indexName, Type searchModelType)
        {
            //TODO: Validate indexName
            if (mRegisteredIndexes.ContainsKey(indexName))
            {
                //TODO: Log a warning when trying to register an index multiple times
            }
            else
            {
                mRegisteredIndexes.Add(indexName, searchModelType);
            }
        }


        /// <summary>
        /// Converts a string to camel case.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <returns>The original <paramref name="input"/> converted to camel case.</returns>
        public static string ConvertToCamelCase(string input)
        {
            //TODO: Validate input
            return Regex.Replace(input, @"([A-Z])([A-Z]+|[a-z0-9_]+)($|[A-Z]\w*)", m =>
            {
                return m.Groups[1].Value.ToLower() + m.Groups[2].Value.ToLower() + m.Groups[3].Value;
            });
        }


        /// <summary>
        /// Gets the <see cref="AlgoliaOptions"/> from the web.config appSettings section.
        /// </summary>
        private static AlgoliaOptions GetAlgoliaOptionsFramework()
        {
            var appSettingService = Service.Resolve<IAppSettingsService>();
            var applicationId = ValidationHelper.GetString(appSettingService["AlgoliaApplicationId"], String.Empty);
            var apiKey = ValidationHelper.GetString(appSettingService["AlgoliaApiKey"], String.Empty);

            return new AlgoliaOptions()
            {
                ApiKey = apiKey,
                ApplicationId = applicationId
            };
        }


        /// <summary>
        /// Gets the <see cref="AlgoliaOptions"/> from the appSettings.json file.
        /// </summary>
        private static AlgoliaOptions GetAlgoliaOptionsCore()
        {
            var configuration = Service.Resolve<IConfiguration>();
            var options = configuration.GetSection(AlgoliaOptions.SECTION_NAME).Get<AlgoliaOptions>();

            return options;
        }
    }
}