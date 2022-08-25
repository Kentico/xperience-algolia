using Algolia.Search.Clients;
using Algolia.Search.Models.Settings;

using CMS.DocumentEngine;

using Kentico.Xperience.Algolia.KX13.Attributes;
using Kentico.Xperience.Algolia.KX13.Models;

using System;
using System.Collections.Generic;

namespace Kentico.Xperience.Algolia.KX13.Services
{
    /// <summary>
    /// Stores the registered Algolia indexes in memory and contains methods for retrieving information
    /// about the registered indexes.
    /// </summary>
    public interface IAlgoliaRegistrationService
    {
        /// <summary>
        /// A collection of registered Algolia indexes.
        /// </summary>
        List<AlgoliaIndex> RegisteredIndexes
        {
            get;
        }


        /// <summary>
        /// Gets the <see cref="IndexSettings"/> of the Algolia index.
        /// </summary>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <returns>The index settings, or null if not found.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        IndexSettings GetIndexSettings(string indexName);


        /// <summary>
        /// Gets the indexed page columns specified by the the index's search model properties for
        /// use when checking whether an indexed column was updated after a page update. The names
        /// of properties with the <see cref="SourceAttribute"/> are ignored, and instead the array
        /// of sources is added to the list of indexed columns.
        /// </summary>
        /// <param name="indexName">The code name of the Algolia index.</param>
        /// <returns>The names of the database columns that are indexed, or an empty array.</returns>
        string[] GetIndexedColumnNames(string indexName);


        /// <summary>
        /// Returns true if the passed node is included in any registered Algolia index.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to check for indexing.</param>
        /// <exception cref="ArgumentNullException"></exception>
        bool IsNodeAlgoliaIndexed(TreeNode node);


        /// <summary>
        /// Returns true if the <paramref name="node"/> is included in the Algolia index's allowed
        /// paths as set by the <see cref="IncludedPathAttribute"/>.
        /// </summary>
        /// <remarks>Logs an error if the search model cannot be found.</remarks>
        /// <param name="node">The node to check for indexing.</param>
        /// <param name="indexName">The Algolia index code name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        bool IsNodeIndexedByIndex(TreeNode node, string indexName);


        /// <summary>
        /// Scans all discoverable assemblies for instances of <see cref="RegisterAlgoliaIndexAttribute"/>s
        /// and stores the <see cref="AlgoliaIndex"/> in memory, in addition to all indexes in the
        /// <see cref="IAlgoliaIndexRegister"/>.
        /// </summary>
        void RegisterAlgoliaIndexes();


        /// <summary>
        /// Stores an <see cref="AlgoliaIndex"/> in memory based on the provided parameters. Also calls
        /// <see cref="SearchIndex.SetSettings"/> to initialize the Algolia index's configuration
        /// based on the attributes defined in the <paramref name="searchModel"/>.
        /// </summary>
        /// <param name="searchModel">The type of the class which extends <see cref="AlgoliaSearchModel"/>.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        /// <param name="siteNames">The code names of the sites whose pages will be included in the index.
        /// If empty, all sites are included.</param>
        /// <remarks>Logs an error if the index settings cannot be loaded.</remarks>
        void RegisterIndex(Type searchModel, string indexName, IEnumerable<string> siteNames = null);
    }
}
