using System;

namespace Kentico.Xperience.AlgoliaSearch.Attributes
{
    /// <summary>
    /// A class attribute applied to an Algolia search model indicating that the specified path and
    /// page type(s) are included in the index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class IncludedPathAttribute : Attribute
    {
        /// <summary>
        /// The node alias pattern that will be used to match pages in the content tree for indexing.
        /// </summary>
        /// <remarks>For example, "/Blogs/Products/%" will index all pages under the "Products" page.</remarks>
        public string AliasPath
        {
            get;
            set;
        }


        /// <summary>
        /// A list of page types under the specified <see cref="AliasPath"/> that will be indexed.
        /// If empty, all page types are indexed.
        /// </summary>
        public string[] PageTypes
        {
            get;
            set;
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="path">The node alias pattern that will be used to match pages in the content tree
        /// for indexing.</param>
        /// <param name="pageTypes">A list of page types under the specified <see cref="AliasPath"/> that
        /// will be indexed. If not provided, all page types are indexed.</param>
        public IncludedPathAttribute(string path, string[] pageTypes = null)
        {
            AliasPath = path;
            PageTypes = (pageTypes == null ? Array.Empty<string>() : pageTypes);
        }
    }
}