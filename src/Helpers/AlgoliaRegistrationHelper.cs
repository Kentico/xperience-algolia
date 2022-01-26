using Algolia.Search.Clients;
using Algolia.Search.Models.Settings;

using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;
using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kentico.Xperience.AlgoliaSearch.Helpers
{
    /// <summary>
    /// Stores the registered Algolia indexes in memory and contains methods for retrieving information
    /// about the registered indexes.
    /// </summary>
    public class AlgoliaRegistrationHelper
    {
        private static IEventLogService mEventLogService;
        private static Dictionary<string, Type> mRegisteredIndexes = new Dictionary<string, Type>();
        

        private static IEventLogService LogService
        {
            get
            {
                if (mEventLogService == null)
                {
                    mEventLogService = Service.Resolve<IEventLogService>();
                }

                return mEventLogService;
            }
        }


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
        /// Gets all <see cref="RegisterAlgoliaIndexAttribute"/>s present in the provided assembly.
        /// </summary>
        /// <param name="assembly">The assembly to scan for attributes.</param>
        public static IEnumerable<RegisterAlgoliaIndexAttribute> GetAlgoliaIndexAttributes(Assembly assembly)
        {
            var attributes = Enumerable.Empty<RegisterAlgoliaIndexAttribute>();

            try
            {
                attributes = assembly.GetCustomAttributes(typeof(RegisterAlgoliaIndexAttribute), false)
                                    .Cast<RegisterAlgoliaIndexAttribute>();
            }
            catch (Exception exception)
            {
                LogService.LogError(nameof(AlgoliaRegistrationHelper), nameof(GetAlgoliaIndexAttributes), $"Failed to register Algolia indexes for assembly '{assembly.FullName}:' {exception.Message}.");
            }

            return attributes;
        }


        /// <summary>
        /// Gets the <see cref="IndexSettings"/> of the Algolia index.
        /// </summary>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <returns>The index settings, or null if not found.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IndexSettings GetIndexSettings(string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            var searchModelType = GetModelByIndexName(indexName);
            if (searchModelType == null)
            {
                LogService.LogError(nameof(AlgoliaRegistrationHelper), nameof(GetIndexSettings), $"Unable to load search model class for index '{indexName}.'");
                return null;
            }

            var searchableProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(SearchableAttribute)));
            var retrievablProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(RetrievableAttribute)));
            var facetableProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(FacetableAttribute)));
            ;
            return new IndexSettings()
            {
                SearchableAttributes = AlgoliaSearchHelper.OrderSearchableProperties(searchableProperties),
                AttributesToRetrieve = retrievablProperties.Select(p => p.Name).ToList(),
                AttributesForFaceting = facetableProperties.Select(AlgoliaSearchHelper.GetFilterablePropertyName).ToList()
            };
        }


        /// <summary>
        /// Gets the registered search model class that is paired with the Algolia index.
        /// </summary>
        /// <param name="indexName">The code name of the Algolia index.</param>
        /// <returns>The search model class type, or null if not found.</returns>
        public static Type GetModelByIndexName(string indexName)
        {
            var records = mRegisteredIndexes.Where(i => i.Key == indexName);
            if (records.Count() == 0)
            {
                return null;
            }

            return records.FirstOrDefault().Value;
        }


        /// <summary>
        /// Gets the indexed page columns specified by the the index's search model properties.
        /// The names of properties with the <see cref="SourceAttribute"/> are ignored, and instead
        /// the array of sources is added to the list of indexed columns.
        /// </summary>
        /// <param name="indexName">The code name of the Algolia index.</param>
        /// <returns>The names of the database columns that are indexed.</returns>
        public static string[] GetIndexedColumnNames(string indexName)
        {
            var searchModelType = GetModelByIndexName(indexName);
            if (searchModelType == null)
            {
                return new string[] { };
            }

            // Don't include properties with SourceAttribute at first, check the sources and add to list after
            var indexedColumnNames = searchModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => !Attribute.IsDefined(prop, typeof(SourceAttribute))).Select(prop => prop.Name).ToList();
            var propertiesWithSourceAttribute = searchModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
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
            var columnsToRemove = new string[] {
                nameof(AlgoliaSearchModel.ObjectID),
                nameof(AlgoliaSearchModel.Url),
                nameof(AlgoliaSearchModel.ClassName)
            };
            indexedColumnNames.RemoveAll(col => columnsToRemove.Contains(col));

            return indexedColumnNames.ToArray();
        }


        /// <summary>
        /// Returns true if the passed node is included in any registered Algolia index.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to check for indexing.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsNodeAlgoliaIndexed(TreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
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
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsNodeIndexedByIndex(TreeNode node, string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var searchModelType = GetModelByIndexName(indexName);
            if (searchModelType == null)
            {
                LogService.LogError(nameof(AlgoliaRegistrationHelper), nameof(IsNodeIndexedByIndex), $"Error loading search model class for index '{indexName}.'");
                return false;
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
        /// Scans all discoverable assemblies for instances of <see cref="RegisterAlgoliaIndexAttribute"/>s
        /// and stores the Algolia index name and search model class in memory. Also calls
        /// <see cref="SearchIndex.SetSettings"/> to initialize the Algolia index's configuration
        /// based on the attributes defined in the search model.
        /// </summary>
        public static void RegisterAlgoliaIndexes()
        {
            var attributes = new List<RegisterAlgoliaIndexAttribute>();
            var assemblies = AssemblyDiscoveryHelper.GetAssemblies(discoverableOnly: true);
            var configuration = Service.ResolveOptional<IConfiguration>();
            var client = AlgoliaSearchHelper.GetSearchClient(configuration);

            foreach (var assembly in assemblies)
            {
                attributes.AddRange(GetAlgoliaIndexAttributes(assembly));
            }

            foreach (var attribute in attributes)
            {
                RegisterIndex(attribute.IndexName, attribute.Type);

                // Set index settings
                var searchIndex = client.InitIndex(attribute.IndexName);
                var indexSettings = GetIndexSettings(attribute.IndexName);
                if (indexSettings == null)
                {
                    LogService.LogError(nameof(AlgoliaRegistrationHelper), nameof(RegisterAlgoliaIndexes), $"Unable to load search index settings for index '{attribute.IndexName}.'");
                    continue;
                }

                searchIndex.SetSettings(indexSettings);
            }
        }


        /// <summary>
        /// Saves an Algolia index code name and its search model to the <see cref="RegisteredIndexes"/>.
        /// </summary>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <param name="searchModelType">The search model type.</param>
        public static void RegisterIndex(string indexName, Type searchModelType)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                LogService.LogError(nameof(AlgoliaRegistrationHelper), nameof(RegisterIndex), "Cannot register Algolia index with empty or null code name.");
                return;
            }

            if (searchModelType == null)
            {
                LogService.LogError(nameof(AlgoliaRegistrationHelper), nameof(RegisterIndex), "Cannot register Algolia index with null search model class.");
                return;
            }

            if (mRegisteredIndexes.ContainsKey(indexName))
            {
                LogService.LogError(nameof(AlgoliaRegistrationHelper), nameof(RegisterIndex), $"Attempted to register Algolia index with name '{indexName},' but it is already registered.");
                return;
            }

            mRegisteredIndexes.Add(indexName, searchModelType);
        }
    }
}
