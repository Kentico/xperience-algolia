using Algolia.Search.Models.Settings;

using CMS.DocumentEngine;

using Kentico.Xperience.AlgoliaSearch.Attributes;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Stores the registered Algolia indexes in memory and contains methods for retrieving information
    /// about the registered indexes.
    /// </summary>
    public interface IAlgoliaRegistrationService
    {
        /// <summary>
        /// A collection of registered Algolia indexes as identified by the
        /// <see cref="RegisterAlgoliaIndexAttribute"/> in the search model class.
        /// </summary>
        public List<RegisterAlgoliaIndexAttribute> RegisteredIndexes
        {
            get;
        }


        /// <summary>
        /// Gets all <see cref="RegisterAlgoliaIndexAttribute"/>s present in the provided assembly.
        /// </summary>
        /// <remarks>Logs an error if the were issues scanning the assembly.</remarks>
        /// <param name="assembly">The assembly to scan for attributes.</param>
        public IEnumerable<RegisterAlgoliaIndexAttribute> GetAlgoliaIndexAttributes(Assembly assembly);


        /// <summary>
        /// Gets the <see cref="IndexSettings"/> of the Algolia index.
        /// </summary>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <returns>The index settings, or null if not found.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public IndexSettings GetIndexSettings(string indexName);


        /// <summary>
        /// Gets the registered search model class that is paired with the Algolia index.
        /// </summary>
        /// <param name="indexName">The code name of the Algolia index.</param>
        /// <returns>The search model class type, or null if not found.</returns>
        public Type GetModelByIndexName(string indexName);


        /// <summary>
        /// Gets the indexed page columns specified by the the index's search model properties for
        /// use when checking whether an indexed column was updated after a page update. The names
        /// of properties with the <see cref="SourceAttribute"/> are ignored, and instead the array
        /// of sources is added to the list of indexed columns.
        /// </summary>
        /// <param name="indexName">The code name of the Algolia index.</param>
        /// <returns>The names of the database columns that are indexed, or an empty array.</returns>
        public string[] GetIndexedColumnNames(string indexName);


        /// <summary>
        /// Returns true if the passed node is included in any registered Algolia index.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to check for indexing.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public bool IsNodeAlgoliaIndexed(TreeNode node);


        /// <summary>
        /// Returns true if the <paramref name="node"/> is included in the Algolia index's allowed
        /// paths as set by the <see cref="IncludedPathAttribute"/>.
        /// </summary>
        /// <remarks>Logs an error if the search model cannot be found.</remarks>
        /// <param name="node">The node to check for indexing.</param>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public bool IsNodeIndexedByIndex(TreeNode node, string indexName);


        /// <summary>
        /// Scans all discoverable assemblies for instances of <see cref="RegisterAlgoliaIndexAttribute"/>s
        /// and stores the Algolia index name and search model class in memory. Also calls
        /// <see cref="SearchIndex.SetSettings"/> to initialize the Algolia index's configuration
        /// based on the attributes defined in the search model.
        /// </summary>
        /// <remarks>Logs an error if the index settings cannot be loaded.</remarks>
        public void RegisterAlgoliaIndexes();


        /// <summary>
        /// Saves an Algolia index's configuration to <see cref="RegisteredIndexes"/>.
        /// </summary>
        /// <remarks>Logs errors if the parameters are invalid, or the index is already registered.</remarks>
        /// <param name="registerAlgoliaIndexAttribute">The <see cref="RegisterAlgoliaIndexAttribute"/> defined
        /// in the search model code file.</param>
        public void RegisterIndex(RegisterAlgoliaIndexAttribute registerAlgoliaIndexAttribute);
    }
}
