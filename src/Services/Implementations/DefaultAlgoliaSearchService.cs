using Algolia.Search.Clients;
using Algolia.Search.Models.Common;

using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models.Facets;
using Kentico.Xperience.AlgoliaSearch.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[assembly: RegisterImplementation(typeof(IAlgoliaSearchService), typeof(DefaultAlgoliaSearchService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaSearchService"/>.
    /// </summary>
    internal class DefaultAlgoliaSearchService : IAlgoliaSearchService
    {
        private readonly ISearchClient searchClient;
        private readonly IAppSettingsService appSettingsService;
        private const string CMS_SETTINGS_KEY_INDEXING_ENABLED = "AlgoliaSearchEnableIndexing";
        private const string APP_SETTINGS_KEY_INDEXING_DISABLED = "AlgoliaSearchDisableIndexing";


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAlgoliaSearchService"/> class.
        /// </summary>
        public DefaultAlgoliaSearchService(ISearchClient searchClient, IAppSettingsService appSettingsService)
        {
            this.searchClient = searchClient;
            this.appSettingsService = appSettingsService;
        }


        public List<IndicesResponse> GetStatistics()
        {
            if (searchClient == null)
            {
                return Enumerable.Empty<IndicesResponse>().ToList();
            }

            return searchClient.ListIndices().Items;
        }


        public AlgoliaFacetedAttribute[] GetFacetedAttributes(Dictionary<string, Dictionary<string, long>> facetsFromResponse, IAlgoliaFacetFilter filter = null, bool displayEmptyFacets = true)
        {
            // Get facets in filter that are checked to persist checked state
            var checkedFacetValues = new List<string>();
            if (filter != null)
            {
                foreach (var facetedAttribute in filter.FacetedAttributes)
                {
                    checkedFacetValues.AddRange(facetedAttribute.Facets.Where(facet => facet.IsChecked).Select(facet => facet.Value));
                }
            }

            var facets = facetsFromResponse.Select(dict =>
                new AlgoliaFacetedAttribute
                {
                    Attribute = dict.Key,
                    DisplayName = dict.Key,
                    Facets = dict.Value.Select(facet =>
                        new AlgoliaFacet
                        {
                            Attribute = dict.Key,
                            Value = facet.Key,
                            DisplayValue = facet.Key,
                            Count = facet.Value,
                            IsChecked = checkedFacetValues.Contains(facet.Key)
                        }
                    ).ToArray()
                }
            ).ToList();

            if (!displayEmptyFacets)
            {
                return facets.ToArray();
            }

            // Loop through all facets present in previous filter
            foreach (var previousFacetedAttribute in filter.FacetedAttributes)
            {
                var matchingFacetFromResponse = facets.Where(facet => facet.Attribute == previousFacetedAttribute.Attribute).FirstOrDefault();
                if (matchingFacetFromResponse == null)
                {
                    // Previous attribute was not returned by Algolia, add to new facet list
                    facets.Add(previousFacetedAttribute);
                    continue;
                }

                // Loop through each facet value in previous facet attribute
                foreach(var previousFacet in previousFacetedAttribute.Facets)
                {
                    if(matchingFacetFromResponse.Facets.Select(facet => facet.Value).Contains(previousFacet.Value))
                    {
                        // The facet value was returned by Algolia search, don't add
                        continue;
                    }

                    // The facet value wasn't returned by Algolia search, display with 0 count
                    previousFacet.Count = 0;
                    var newFacetList = matchingFacetFromResponse.Facets.ToList();
                    newFacetList.Add(previousFacet);
                    matchingFacetFromResponse.Facets = newFacetList.ToArray();
                }
            }

            // Sort the facet values. Usually handled by Algolia, but we are modifying the list
            foreach(var facetedAttribute in facets)
            {
                facetedAttribute.Facets = facetedAttribute.Facets.OrderBy(facet => facet.Value).ToArray();
            }

            return facets.ToArray();
        }


        public string GetFilterablePropertyName(PropertyInfo property)
        {
            var attr = property.GetCustomAttributes<FacetableAttribute>(false).FirstOrDefault();
            if (attr.FilterOnly && attr.Searchable)
            {
                throw new InvalidOperationException("Facetable attributes cannot be both searchable and filterOnly.");
            }

            if (attr.FilterOnly)
            {
                return $"filterOnly({property.Name})";
            }
            if (attr.Searchable)
            {
                return $"searchable({property.Name})";
            }

            return property.Name;
        }


        public bool IsIndexingEnabled()
        {
            var indexingDisabled = ValidationHelper.GetBoolean(appSettingsService[APP_SETTINGS_KEY_INDEXING_DISABLED], false);
    
            if (indexingDisabled)
            {
                return false;
            }

            var existingKey = SettingsKeyInfoProvider.GetSettingsKeyInfo(CMS_SETTINGS_KEY_INDEXING_ENABLED);
            if (existingKey == null)
            {
                return true;
            }

            return ValidationHelper.GetBoolean(existingKey.KeyValue, true);
        }


        public List<string> OrderSearchableProperties(IEnumerable<PropertyInfo> searchableProperties)
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
                    if (prop.Value.Unordered)
                    {
                        return $"unordered({prop.Key})";
                    }

                    return prop.Key;
                }).Join(",")
            ).ToList();

            // Add properties without order as single items
            var propertiesWithoutOrdering = propertiesWithAttribute.Where(prop => prop.Value.Order == -1);
            foreach (var prop in propertiesWithoutOrdering)
            {
                if (prop.Value.Unordered)
                {
                    searchableAttributes.Add($"unordered({prop.Key})");
                    continue;
                }

                searchableAttributes.Add(prop.Key);
            }

            return searchableAttributes;
        }
    }
}