using Algolia.Search.Clients;
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
        /// Gets an instance of <see cref="SearchIndex"/> for the specified Algolia index.
        /// </summary>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <returns>A <see cref="SearchIndex"/> to search with, or null if not found.</returns>
        public static SearchIndex GetSearchIndex(string indexName)
        {
            //TODO: Validate indexName
            SearchClient client = null;
            if (SystemContext.IsCMSRunningAsMainApplication)
            {
                client = GetSearchClientFramework();
            }
            else
            {
                client = GetSearchClientCore();
            }
            
            if (client == null)
            {
                //TODO: Throw or log error
            }

            return client.InitIndex(indexName);
        }


        /// <summary>
        /// Gets the <see cref="IndexSettings"/> of the Algolia index.
        /// </summary>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <returns>The index settings.</returns>
        public static IndexSettings GetIndexSettings(string indexName)
        {
            //TODO: Validate indexName
            var modelType = GetModelByIndexName(indexName);
            if (modelType == null)
            {
                //TODO: Throw or log error
            }

            var searchableAttributes = GetSearchModelAttributes(modelType, typeof(SearchableAttribute));
            var retrievableAttributes = GetSearchModelAttributes(modelType, typeof(RetrievableAttribute));
            var facetableAttributes = GetSearchModelAttributes(modelType, typeof(FacetableAttribute));

            return new IndexSettings()
            {
                SearchableAttributes = searchableAttributes,
                AttributesToRetrieve = retrievableAttributes,
                AttributesForFaceting = facetableAttributes
            };
        }


        /// <summary>
        /// Gets the properties of the specified <paramref name="searchModelType"/> which have the specified
        /// <paramref name="attributeType"/> attribute applied.
        /// </summary>
        /// <param name="searchModelType">The search model class type to search.</param>
        /// <param name="attributeType">The type of the attribute to search for.</param>
        /// <returns>A list of property names converted to camel case.</returns>
        public static List<string> GetSearchModelAttributes(Type searchModelType, Type attributeType)
        {
            var propertiesWithAttribute = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, attributeType));
            return propertiesWithAttribute.Select(prop => ConvertToCamelCase(prop.Name)).ToList();
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
                var allowedPageTypes = includedPathAttribute.PageTypes;
                if (path.EndsWith("/%"))
                {
                    path = path.TrimEnd('%', '/');
                    if (node.NodeAliasPath.StartsWith(path) &&
                        (allowedPageTypes.Length == 0 || allowedPageTypes.Contains(node.ClassName)))
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


        private static SearchClient GetSearchClientFramework()
        {
            var appSettingService = Service.Resolve<IAppSettingsService>();
            var applicationId = ValidationHelper.GetString(appSettingService["AlgoliaApplicationId"], String.Empty);
            var apiKey = ValidationHelper.GetString(appSettingService["AlgoliaApiKey"], String.Empty);

            //TODO: Validate options
            return new SearchClient(applicationId, apiKey);
        }


        private static SearchClient GetSearchClientCore()
        {
            var configuration = Service.Resolve<IConfiguration>();
            var options = configuration.GetSection(AlgoliaOptions.SECTION_NAME).Get<AlgoliaOptions>();

            //TODO: Validate options
            return new SearchClient(options.ApplicationId, options.ApiKey);
        }
    }
}