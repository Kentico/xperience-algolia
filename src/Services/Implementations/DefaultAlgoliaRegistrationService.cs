using Algolia.Search.Clients;
using Algolia.Search.Models.Settings;

using CMS;
using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.Algolia.KX13.Attributes;
using Kentico.Xperience.Algolia.KX13.Models;
using Kentico.Xperience.Algolia.KX13.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: RegisterImplementation(typeof(IAlgoliaRegistrationService), typeof(DefaultAlgoliaRegistrationService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.Algolia.KX13.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaRegistrationService"/>.
    /// </summary>
    internal class DefaultAlgoliaRegistrationService : IAlgoliaRegistrationService
    {
        private readonly IAlgoliaSearchService algoliaSearchService;
        private readonly IEventLogService eventLogService;
        private readonly ISearchClient searchClient;
        private readonly IAlgoliaIndexService algoliaIndexService;
        private readonly IAlgoliaIndexRegister algoliaIndexRegister;
        private readonly List<AlgoliaIndex> mRegisteredIndexes = new List<AlgoliaIndex>();
        private readonly string[] ignoredPropertiesForTrackingChanges = new string[] {
            nameof(AlgoliaSearchModel.ObjectID),
            nameof(AlgoliaSearchModel.Url),
            nameof(AlgoliaSearchModel.ClassName)
        };


        public List<AlgoliaIndex> RegisteredIndexes
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
            ISearchClient searchClient,
            IAlgoliaIndexService algoliaIndexService,
            IAlgoliaIndexRegister algoliaIndexRegister)
        {
            this.algoliaSearchService = algoliaSearchService;
            this.eventLogService = eventLogService;
            this.searchClient = searchClient;
            this.algoliaIndexService = algoliaIndexService;
            this.algoliaIndexRegister = algoliaIndexRegister;
        }


        public IndexSettings GetIndexSettings(string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            var alogliaIndex = mRegisteredIndexes.FirstOrDefault(i => i.IndexName == indexName);
            if (alogliaIndex == null)
            {
                return null;
            }

            var searchableProperties = alogliaIndex.Type.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(SearchableAttribute)));
            var retrievablProperties = alogliaIndex.Type.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(RetrievableAttribute)));
            var facetableProperties = alogliaIndex.Type.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(FacetableAttribute)));
            
            return new IndexSettings()
            {
                SearchableAttributes = algoliaSearchService.OrderSearchableProperties(searchableProperties),
                AttributesToRetrieve = retrievablProperties.Select(p => p.Name).ToList(),
                AttributesForFaceting = facetableProperties.Select(algoliaSearchService.GetFilterablePropertyName).ToList()
            };
        }


        public string[] GetIndexedColumnNames(string indexName)
        {
            var alogliaIndex = mRegisteredIndexes.FirstOrDefault(i => i.IndexName == indexName);
            if (alogliaIndex == null)
            {
                return new string[] { };
            }

            // Don't include properties with SourceAttribute at first, check the sources and add to list after
            var indexedColumnNames = alogliaIndex.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => !Attribute.IsDefined(prop, typeof(SourceAttribute))).Select(prop => prop.Name).ToList();
            var propertiesWithSourceAttribute = alogliaIndex.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
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

            var alogliaIndex = mRegisteredIndexes.FirstOrDefault(i => i.IndexName == indexName);
            if (alogliaIndex == null)
            {
                eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(IsNodeIndexedByIndex), $"Error loading registered Algolia index '{indexName}.'");
                return false;
            }

            if (alogliaIndex.SiteNames != null && !alogliaIndex.SiteNames.Contains(node.NodeSiteName))
            {
                return false;
            }
            
            var includedPathAttributes = alogliaIndex.Type.GetCustomAttributes<IncludedPathAttribute>(false);
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
                RegisterIndex(attribute.Type, attribute.IndexName, attribute.SiteNames);
            }

            var algoliaIndex = algoliaIndexRegister.Pop();
            while (algoliaIndex != null)
            {
                RegisterIndex(algoliaIndex.Type, algoliaIndex.IndexName, algoliaIndex.SiteNames);
                algoliaIndex = algoliaIndexRegister.Pop();
            }
        }


        public void RegisterIndex(Type searchModel, string indexName, IEnumerable<string> siteNames = null)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(RegisterIndex), "Cannot register Algolia index with empty or null code name.");
                return;
            }

            if (searchModel == null)
            {
                eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(RegisterIndex), "Cannot register Algolia index with null search model type.");
                return;
            }

            if (mRegisteredIndexes.Any(i => i.IndexName == indexName))
            {
                eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(RegisterIndex), $"Attempted to register Algolia index with name '{indexName},' but it is already registered.");
                return;
            }

            var algoliaIndex = new AlgoliaIndex
            {
                IndexName = indexName,
                Type = searchModel,
                SiteNames = siteNames
            };
            try
            {
                mRegisteredIndexes.Add(algoliaIndex);

                var searchIndex = algoliaIndexService.InitializeIndex(indexName);
                var indexSettings = GetIndexSettings(indexName);
                if (indexSettings == null)
                {
                    eventLogService.LogError(nameof(DefaultAlgoliaRegistrationService), nameof(RegisterIndex), $"Unable to load search index settings for index '{indexName}.'");
                    return;
                }

                searchIndex.SetSettings(indexSettings);
            }
            catch (Exception ex)
            {
                mRegisteredIndexes.Remove(algoliaIndex);
                eventLogService.LogException(nameof(DefaultAlgoliaRegistrationService), nameof(RegisterIndex), ex, additionalMessage: $"Cannot register Algolia index '{indexName}.'");
            }
        }


        private IEnumerable<RegisterAlgoliaIndexAttribute> GetAlgoliaIndexAttributes(Assembly assembly)
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
    }
}
