﻿using Algolia.Search.Clients;
using Algolia.Search.Models.Common;
using Algolia.Search.Models.Search;
using Algolia.Search.Models.Settings;

using CMS.Base;
using CMS.Core;
using CMS.DocumentEngine;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Models.Facets;
using Kentico.Xperience.AlgoliaSearch.Attributes;

using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kentico.Xperience.AlgoliaSearch.Helpers
{
    /// <summary>
    /// Contains methods for common Algolia tasks and stores all registered Algolia indexes.
    /// </summary>
    public class AlgoliaSearchHelper
    {
        private static Dictionary<string, Type> mRegisteredIndexes = new Dictionary<string, Type>();
        private static IEventLogService mEventLogService;


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
        /// Gets an <see cref="AlgoliaOptions"/> object with the Algolia settings specified in
        /// either the web.config or appsettings.json, depending on the application.
        /// <param name="configuration">The <see cref="IConfiguration"/> of the .NET Core
        /// application, or null if running from the Xperience application.</param>
        /// </summary>
        public static AlgoliaOptions GetAlgoliaOptions(IConfiguration configuration = null)
        {
            if (SystemContext.IsCMSRunningAsMainApplication)
            {
                return GetAlgoliaOptionsFramework();
            }

            return GetAlgoliaOptionsCore(configuration);
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
        /// Gets a <see cref="SearchClient"/> for performing Algolia search methods.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> of the .NET Core
        /// application, or null if running from the Xperience application.</param>
        /// <returns>An Algolia <see cref="SearchClient"/>, or null if there was an error
        /// creating it.</returns>
        public static SearchClient GetSearchClient(IConfiguration configuration = null)
        {
            var options = GetAlgoliaOptions(configuration);
            if (options == null || String.IsNullOrEmpty(options.ApplicationId) || String.IsNullOrEmpty(options.ApiKey))
            {
                LogService.LogError(nameof(AlgoliaSearchHelper), nameof(GetSearchClient), "Unable to load Algolia configuration keys.");
                return null;
            }

            return new SearchClient(options.ApplicationId, options.ApiKey);
        }


        /// <summary>
        /// Gets the indices of the Algolia application with basic statistics.
        /// </summary>
        /// <remarks>See <see href="https://www.algolia.com/doc/api-reference/api-methods/list-indices/#response"/></remarks>
        public static List<IndicesResponse> GetStatistics()
        {
            var configuration = Service.ResolveOptional<IConfiguration>();
            var client = GetSearchClient(configuration);
            if (client == null)
            {
                return Enumerable.Empty<IndicesResponse>().ToList();
            }

            return client.ListIndices().Items;
        }


        /// <summary>
        /// Gets a list of faceted Algolia attributes from a search response. If a <paramref name="filter"/> is
        /// provided, the <see cref="AlgoliaFacet.IsChecked"/> property is set based on the state of the filter.
        /// </summary>
        /// <param name="facetsFromResponse">The <see cref="SearchResponse{T}.Facets"/> returned from an Algolia search.</param>
        /// <param name="filter">The <see cref="IAlgoliaFacetFilter"/> used in previous Algolia searches, containing
        /// the facets that were present and their <see cref="AlgoliaFacet.IsChecked"/> states.</param>
        /// <param name="displayEmptyFacets">If true, facets that would not return results from Algolia will be added
        /// to the returned list with a count of zero.</param>
        /// <returns>A new list of <see cref="AlgoliaFacetedAttribute"/>s that are available to filter search
        /// results.</returns>
        public static AlgoliaFacetedAttribute[] GetFacetedAttributes(Dictionary<string, Dictionary<string, long>> facetsFromResponse, IAlgoliaFacetFilter filter = null, bool displayEmptyFacets = true)
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
                LogService.LogError(nameof(AlgoliaSearchHelper), nameof(GetIndexSettings), $"Unable to load search model class for index '{indexName}.'");
                return null;
            }

            var searchableProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(SearchableAttribute)));
            var retrievablProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(RetrievableAttribute)));
            var facetableProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(FacetableAttribute)));
            ;
            return new IndexSettings()
            {
                SearchableAttributes = OrderSearchableProperties(searchableProperties),
                AttributesToRetrieve = retrievablProperties.Select(p => p.Name).ToList(),
                AttributesForFaceting = facetableProperties.Select(GetFilterablePropertyName).ToList()
            };
        }


        /// <summary>
        /// Returns true if the <paramref name="node"/> should be indexed based on the configuration
        /// of the <paramref name="includedPathAttribute"/> and the node's ACLs.
        /// </summary>
        /// <param name="includedPathAttribute">An index's path attribute.</param>
        /// <param name="node">The node to check permissions for.</param>
        public static bool NodeMeetsPermissionRequirements(IncludedPathAttribute includedPathAttribute, TreeNode node)
        {
            var nodeRoles = AclItemInfoProvider.GetAllowedRoles(node.NodeACLID, NodePermissionsEnum.Read, "OperatorName");
            var nodeRoleNames = nodeRoles.Tables[0].Select().Select(row => ValidationHelper.GetString(row["OperatorName"], String.Empty));
            foreach (var role in includedPathAttribute.AllowedRoles)
            {
                if (!nodeRoleNames.Contains(role))
                {
                    return false;
                }
            }

            return true;
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
                LogService.LogError(nameof(AlgoliaSearchHelper), nameof(RegisterIndex), "Cannot register Algolia index with empty or null code name.");
                return;
            }

            if (searchModelType == null)
            {
                LogService.LogError(nameof(AlgoliaSearchHelper), nameof(RegisterIndex), "Cannot register Algolia index with null search model class.");
                return;
            }

            if (mRegisteredIndexes.ContainsKey(indexName))
            {
                LogService.LogError(nameof(AlgoliaSearchHelper), nameof(RegisterIndex), $"Attempted to register Algolia index with name '{indexName},' but it is already registered.");
                return;
            }
            
            mRegisteredIndexes.Add(indexName, searchModelType);
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
        /// <param name="configuration">The <see cref="IConfiguration"/> of the .NET Core
        /// application.</param>
        /// </summary>
        private static AlgoliaOptions GetAlgoliaOptionsCore(IConfiguration configuration)
        {
            return configuration.GetSection(AlgoliaOptions.SECTION_NAME).Get<AlgoliaOptions>();
        }


        /// <summary>
        /// Converts a property name with a <see cref="FacetableAttribute"/> into the correct Algolia
        /// format, based on the configured options of the <see cref="FacetableAttribute"/>.
        /// </summary>
        /// <param name="property">The search model property to get the name of.</param>
        /// <returns>The property name marked as either "filterOnly" or "searchable."</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="FacetableAttribute"/>
        /// has both <see cref="FacetableAttribute.FilterOnly"/> and <see cref="FacetableAttribute.Searchable"/>
        /// set to true.</exception>
        private static string GetFilterablePropertyName(PropertyInfo property)
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


        /// <summary>
        /// Returns a list of searchable properties ordered by <see cref="SearchableAttribute.Order"/>,
        /// with properties having the same <see cref="SearchableAttribute.Order"/> in a single string
        /// separated by commas.
        /// </summary>
        /// <param name="searchableProperties">The properties of the search model to be ordered.</param>
        /// <returns>A list of strings appropriate for setting Algolia searchable attributes (see
        /// <see href="https://www.algolia.com/doc/api-reference/api-parameters/searchableAttributes/"/>).</returns>
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