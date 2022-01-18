using Algolia.Search.Clients;

using System;

namespace Kentico.Xperience.AlgoliaSearch.Attributes
{
    /// <summary>
    /// A property attribute to indicate a search model property is facetable within Algolia.
    /// </summary>
    /// <remarks>See <see href="https://www.algolia.com/doc/api-reference/api-parameters/attributesForFaceting/"/>.</remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class FacetableAttribute : Attribute
    {
        /// <summary>
        /// Defines an attribute as filterable only and not facetable. If you only need the
        /// filtering feature, you can take advantage of filterOnly which will reduce the index
        /// size and improve the speed of the search.
        /// </summary>
        public bool FilterOnly
        {
            get;
            set;
        }


        /// <summary>
        /// Defines an attribute as searchable. If you want to search for values of a given facet
        /// (using <see cref="SearchIndex.SearchForFacetValue"/>) you need to specify searchable.
        /// </summary>
        public bool Searchable
        {
            get;
            set;
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filterOnly">Defines an attribute as filterable only and not facetable. If you
        /// only need the filtering feature, you can take advantage of filterOnly which will reduce the
        /// index size and improve the speed of the search.</param>
        /// <param name="searchable"> Defines an attribute as searchable. If you want to search for values
        /// of a given facet (using <see cref="SearchIndex.SearchForFacetValue"/>) you need to specify
        /// searchable.</param>
        public FacetableAttribute(bool filterOnly = false, bool searchable = false)
        {
            FilterOnly = filterOnly;
            Searchable = searchable;
        }
     }
}