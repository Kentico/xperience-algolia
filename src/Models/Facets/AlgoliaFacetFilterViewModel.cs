using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Localization;

namespace Kentico.Xperience.AlgoliaSearch.Models.Facets
{
    /// <summary>
    /// Contains the faceted attributes of an Algolia index and the filter state for
    /// a faceted search interface.
    /// </summary>
    public class AlgoliaFacetFilterViewModel : IAlgoliaFacetFilter
    {
        public AlgoliaFacetedAttribute[] FacetedAttributes { get; set; } = new AlgoliaFacetedAttribute[0];


        /// <summary>
        /// Constructor.
        /// </summary>
        public AlgoliaFacetFilterViewModel()
        {

        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="facets">A collection of an Algolia index's faceted attributes and the
        /// available facets.</param>
        public AlgoliaFacetFilterViewModel(AlgoliaFacetedAttribute[] facets)
        {
            FacetedAttributes = facets;
        }


        public IEnumerable<IEnumerable<string>> GetFilters()
        {
            var checkedFacets = new List<AlgoliaFacet>();
            foreach (var facetedAttribute in FacetedAttributes)
            {
                checkedFacets.AddRange(facetedAttribute.Facets.Where(facet => facet.IsChecked));
            }

            return checkedFacets.Select(facet => new string[] { $"{facet.Attribute}:{facet.Value}" });
        }


        public void Localize(IStringLocalizer localizer)
        {
            foreach (var facetedAttribute in FacetedAttributes)
            {
                facetedAttribute.DisplayName = localizer.GetString($"algolia.facet.{facetedAttribute.Attribute}");
                foreach (var facet in facetedAttribute.Facets)
                {
                    facet.DisplayValue = localizer.GetString($"algolia.facet.{facet.Attribute}.{facet.Value}");
                }
            }
        }
    }
}