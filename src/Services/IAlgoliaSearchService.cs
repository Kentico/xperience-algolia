using Algolia.Search.Models.Common;

using Kentico.Xperience.AlgoliaSearch.Models.Facets;

using System.Collections.Generic;
using System.Reflection;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Contains methods for common Algolia tasks.
    /// </summary>
    public abstract class IAlgoliaSearchService
    {
        protected const string KEY_INDEXING_ENABLED = "AlgoliaSearchEnableIndexing";


        /// <summary>
        /// Gets the indices of the Algolia application with basic statistics.
        /// </summary>
        /// <remarks>See <see href="https://www.algolia.com/doc/api-reference/api-methods/list-indices/#response"/></remarks>
        public abstract List<IndicesResponse> GetStatistics();


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
        public abstract AlgoliaFacetedAttribute[] GetFacetedAttributes(Dictionary<string, Dictionary<string, long>> facetsFromResponse, IAlgoliaFacetFilter filter = null, bool displayEmptyFacets = true);


        /// <summary>
        /// Converts a property name with a <see cref="FacetableAttribute"/> into the correct Algolia
        /// format, based on the configured options of the <see cref="FacetableAttribute"/>.
        /// </summary>
        /// <param name="property">The search model property to get the name of.</param>
        /// <returns>The property name marked as either "filterOnly" or "searchable."</returns>
        /// <exception cref="InvalidOperationException">Thrown if the <see cref="FacetableAttribute"/>
        /// has both <see cref="FacetableAttribute.FilterOnly"/> and <see cref="FacetableAttribute.Searchable"/>
        /// set to true.</exception>
        public abstract string GetFilterablePropertyName(PropertyInfo property);


        /// <summary>
        /// Returns true if Algolia indexing is enabled, or if the settings key doesn't exist.
        /// </summary>
        public abstract bool IsIndexingEnabled();


        /// <summary>
        /// Returns a list of searchable properties ordered by <see cref="SearchableAttribute.Order"/>,
        /// with properties having the same <see cref="SearchableAttribute.Order"/> in a single string
        /// separated by commas.
        /// </summary>
        /// <param name="searchableProperties">The properties of the search model to be ordered.</param>
        /// <returns>A list of strings appropriate for setting Algolia searchable attributes (see
        /// <see href="https://www.algolia.com/doc/api-reference/api-parameters/searchableAttributes/"/>).</returns>
        public abstract List<string> OrderSearchableProperties(IEnumerable<PropertyInfo> searchableProperties);
    }
}
