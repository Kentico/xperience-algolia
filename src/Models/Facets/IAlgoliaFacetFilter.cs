using Algolia.Search.Models.Search;

using Microsoft.Extensions.Localization;

using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Models.Facets
{
    /// <summary>
    /// Defines methods for creating Algolia faceting interfaces and filtering queries based
    /// on the selected facets.
    /// </summary>
    public interface IAlgoliaFacetFilter
    {
        /// <summary>
        /// A collection of an Algolia index's faceted attributes and the available facets.
        /// </summary>
        public abstract AlgoliaFacetedAttribute[] FacetedAttributes { get; set; }


        /// <summary>
        /// Gets a collection of facet filters to be used in <see cref="Query.FacetFilters"/> to
        /// filter an Algolia search based on selected facets and their values.
        /// </summary>
        IEnumerable<IEnumerable<string>> GetFilters();


        /// <summary>
        /// Sets the <see cref="AlgoliaFacetedAttribute.DisplayName"/> of each facet within
        /// <see cref="FacetedAttributes"/>. The key searched within the given <see cref="IStringLocalizer"/>
        /// is in the format <i>algolia.facet.[AttributeName]</i>.
        /// </summary>
        /// <param name="localizer">The localizer containing facet display names. See
        /// <see href="https://docs.xperience.io/multilingual-websites/setting-up-a-multilingual-user-interface/localizing-builder-components"/>.</param>
        void Localize(IStringLocalizer localizer);
    }
}
