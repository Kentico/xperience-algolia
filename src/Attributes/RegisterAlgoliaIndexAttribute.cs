using Kentico.Xperience.AlgoliaSearch.Models;

using System;

namespace Kentico.Xperience.AlgoliaSearch.Attributes
{
    /// <summary>
    /// When applied to a class extending <see cref="AlgoliaSearchModel"/>, the Algolia index
    /// and its configuration will be registered during startup to enable indexing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class RegisterAlgoliaIndexAttribute : Attribute
    {
        /// <summary>
        /// The type of the class which extends <see cref="AlgoliaSearchModel"/>.
        /// </summary>
        public Type Type
        {
            get;
        }


        /// <summary>
        /// The code name of the Algolia index.
        /// </summary>
        public string IndexName
        {
            get;
        }


        /// <summary>
        /// The code names of the sites whose pages will be included in the index. If empty, all sites
        /// are included.
        /// </summary>
        public string[] SiteNames
        {
            get;
            set;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterAlgoliaIndexAttribute"/> class.
        /// </summary>
        /// <param name="type">The type of the class which extends <see cref="AlgoliaSearchModel"/>.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        public RegisterAlgoliaIndexAttribute(Type type, string indexName)
        {
            Type = type;
            IndexName = indexName;
        }
    }
}