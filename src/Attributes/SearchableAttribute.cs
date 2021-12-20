using System;

namespace Kentico.Xperience.AlgoliaSearch.Attributes
{
    /// <summary>
    /// A property attribute to indicate a search model property is searchable within Algolia.
    /// </summary>
    /// <remarks>See <see href="https://www.algolia.com/doc/api-reference/api-parameters/searchableAttributes/"/>.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SearchableAttribute : Attribute
    {
    }
}