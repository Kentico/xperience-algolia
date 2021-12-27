using System;

namespace Kentico.Xperience.AlgoliaSearch.Attributes
{
    /// <summary>
    /// A property attribute to indicate a search model property is facetable within Algolia.
    /// </summary>
    /// <remarks>See <see href="https://www.algolia.com/doc/api-reference/api-parameters/attributesForFaceting/"/>.</remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class FacetableAttribute : Attribute
    {
    }
}