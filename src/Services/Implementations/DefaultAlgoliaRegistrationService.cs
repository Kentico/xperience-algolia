using Algolia.Search.Clients;
using Algolia.Search.Models.Settings;

using CMS;
using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: RegisterImplementation(typeof(IAlgoliaRegistrationService), typeof(DefaultAlgoliaRegistrationService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaRegistrationService"/>.
    /// </summary>
    public class DefaultAlgoliaRegistrationService : IAlgoliaRegistrationService
    {
        private readonly IAlgoliaSearchService algoliaSearchService;
        private readonly IEventLogService eventLogService;
        private readonly ISearchClient searchClient;
        private List<RegisterAlgoliaIndexAttribute> mRegisteredIndexes = new List<RegisterAlgoliaIndexAttribute>();
        private string[] ignoredPropertiesForTrackingChanges = new string[] {
            nameof(AlgoliaSearchModel.ObjectID),
            nameof(AlgoliaSearchModel.Url),
            nameof(AlgoliaSearchModel.ClassName)
        };


        public List<RegisterAlgoliaIndexAttribute> RegisteredIndexes
        {
            get
            {
                return mRegisteredIndexes;
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAlgoliaRegistrationService"/> class.
        /// </summary>
        public DefaultAlgoliaRegistrationService(IAlgoliaSearchService algoliaSearchService,
            IEventLogService eventLogService,
            ISearchClient searchClient)
        {
            this.algoliaSearchService = algoliaSearchService;
            this.eventLogService = eventLogService;
            this.searchClient = searchClient;
        }


        public IEnumerable<RegisterAlgoliaIndexAttribute> GetAlgoliaIndexAttributes(Assembly assembly)
        {
            var attributes = Enumerable.Empty<RegisterAlgoliaIndexAttribute>();

            try
            {
                attributes = assembly.GetCustomAttributes(typeof(RegisterAlgoliaIndexAttribute), false)
                                    .Cast<RegisterAlgoliaIndexAttribute>();
            }
            catch (Exception exception)
            {
                eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(GetAlgoliaIndexAttributes), $"Failed to register Algolia indexes for assembly '{assembly.FullName}:' {exception.Message}.");
            }

            return attributes;
        }


        public IndexSettings GetIndexSettings(string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            var registerIndexAttribute = mRegisteredIndexes.FirstOrDefault(i => i.IndexName == indexName);
            if (registerIndexAttribute == null || registerIndexAttribute.Type == null)
            {
                return null;
            }

            var searchableProperties = registerIndexAttribute.Type.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(SearchableAttribute)));
            var retrievablProperties = registerIndexAttribute.Type.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(RetrievableAttribute)));
            var facetableProperties = registerIndexAttribute.Type.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(FacetableAttribute)));
            ;
            return new IndexSettings()
            {
                SearchableAttributes = algoliaSearchService.OrderSearchableProperties(searchableProperties),
                AttributesToRetrieve = retrievablProperties.Select(p => p.Name).ToList(),
                AttributesForFaceting = facetableProperties.Select(algoliaSearchService.GetFilterablePropertyName).ToList()
            };
        }


        public string[] GetIndexedColumnNames(string indexName)
        {
            var registerIndexAttribute = mRegisteredIndexes.FirstOrDefault(i => i.IndexName == indexName);
            if (registerIndexAttribute == null || registerIndexAttribute.Type == null)
            {
                return new string[] { };
            }

            // Don't include properties with SourceAttribute at first, check the sources and add to list after
            var indexedColumnNames = registerIndexAttribute.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => !Attribute.IsDefined(prop, typeof(SourceAttribute))).Select(prop => prop.Name).ToList();
            var propertiesWithSourceAttribute = registerIndexAttribute.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
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

            return indexedColumnNames.ToArray();
        }


        public bool IsNodeAlgoliaIndexed(TreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            foreach (var index in mRegisteredIndexes)
            {
                if (IsNodeIndexedByIndex(node, index.IndexName))
                {
                    return true;
                }
            }

            return false;
        }


        public bool IsNodeIndexedByIndex(TreeNode node, string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var registerIndexAttribute = mRegisteredIndexes.FirstOrDefault(i => i.IndexName == indexName);
            if (registerIndexAttribute == null || registerIndexAttribute.Type == null)
            {
                eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(IsNodeIndexedByIndex), $"Error loading search model class for index '{indexName}.'");
                return false;
            }

            var registrationAttribute = mRegisteredIndexes.FirstOrDefault(i => i.IndexName == indexName);
            if (registrationAttribute != null)
            {
                if (registrationAttribute.SiteNames != null && !registrationAttribute.SiteNames.Contains(node.NodeSiteName))
                {
                    return false;
                }
            }

            var includedPathAttributes = registerIndexAttribute.Type.GetCustomAttributes<IncludedPathAttribute>(false);
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


        public void RegisterAlgoliaIndexes()
        {
            var attributes = new List<RegisterAlgoliaIndexAttribute>();
            var assemblies = AssemblyDiscoveryHelper.GetAssemblies(discoverableOnly: true);

            foreach (var assembly in assemblies)
            {
                attributes.AddRange(GetAlgoliaIndexAttributes(assembly));
            }

            foreach (var attribute in attributes)
            {
                try
                {
                    RegisterIndex(attribute);

                    var searchIndex = searchClient.InitIndex(attribute.IndexName);
                    var indexSettings = GetIndexSettings(attribute.IndexName);
                    if (indexSettings == null)
                    {
                        eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(RegisterAlgoliaIndexes), $"Unable to load search index settings for index '{attribute.IndexName}.'");
                        continue;
                    }

                    searchIndex.SetSettings(indexSettings);
                    
                }
                catch (Exception ex)
                {
                    mRegisteredIndexes.Remove(attribute);
                    eventLogService.LogException(nameof(DefaultAlgoliaRegistrationService), nameof(RegisterAlgoliaIndexes), ex, additionalMessage: $"Cannot register Algolia index '{attribute.IndexName}.'");
                }
            }
        }


        public void RegisterIndex(RegisterAlgoliaIndexAttribute registerAlgoliaIndexAttribute)
        {
            if (String.IsNullOrEmpty(registerAlgoliaIndexAttribute.IndexName))
            {
                eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(RegisterIndex), "Cannot register Algolia index with empty or null code name.");
                return;
            }

            if (registerAlgoliaIndexAttribute.Type == null)
            {
                eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(RegisterIndex), "Cannot register Algolia index with null search model class.");
                return;
            }

            if (mRegisteredIndexes.Any(i => i.IndexName == registerAlgoliaIndexAttribute.IndexName))
            {
                eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(RegisterIndex), $"Attempted to register Algolia index with name '{registerAlgoliaIndexAttribute.IndexName},' but it is already registered.");
                return;
            }

            mRegisteredIndexes.Add(registerAlgoliaIndexAttribute);
        }
    }
}
