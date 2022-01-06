﻿namespace Kentico.Xperience.AlgoliaSearch.Models.Facets
{
    /// <summary>
    /// Represents an Algolia faceted attribute's value.
    /// </summary>
    public class AlgoliaFacet
    {
        /// <summary>
        /// The camel-cased code name of the faceted attribute.
        /// </summary>
        public string Attribute { get; set; }

        
        /// <summary>
        /// The value of the facet.
        /// </summary>
        public string Value { get; set; }


        /// <summary>
        /// The number of hits that will be returned when this facet
        /// is used within an Algolia search.
        /// </summary>
        public long Count { get; set; }


        /// <summary>
        /// True if the facet was used in a previous Algolia search.
        /// </summary>
        public bool IsChecked { get; set; }
    }
}