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
        public int Order
        {
            get;
            set;
        }


        public bool Unordered
        {
            get;
            set;
        }

        public SearchableAttribute(int order = -1, bool unordered = false)
        {
            Order = order;
            Unordered = unordered;
        }
    }
}