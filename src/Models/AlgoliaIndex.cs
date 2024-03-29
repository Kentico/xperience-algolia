﻿using System;
using System.Collections.Generic;
using System.Linq;

using Kentico.Xperience.Algolia.Attributes;

namespace Kentico.Xperience.Algolia.Models
{
    /// <summary>
    /// Represents the configuration of an Algolia index.
    /// </summary>
    public sealed class AlgoliaIndex
    {
        /// <summary>
        /// The distinct and de-duplication settings for the Algolia index.
        /// </summary>
        public DistinctOptions DistinctOptions
        {
            get;
            set;
        }


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
        public IEnumerable<string> SiteNames
        {
            get;
        }


        /// <summary>
        /// The <see cref="IncludedPathAttribute"/>s which are defined in the search model.
        /// </summary>
        internal IEnumerable<IncludedPathAttribute> IncludedPaths
        {
            get;
            set;
        }


        /// <summary>
        /// Initializes a new <see cref="AlgoliaIndex"/>.
        /// </summary>
        /// <param name="type">The type of the class which extends <see cref="AlgoliaSearchModel"/>.</param>
        /// <param name="indexName">The code name of the Algolia index.</param>
        /// <param name="distinctOptions">The distinct and de-duplication settings for the Algolia index.</param>
        /// <param name="siteNames">The code names of the sites whose pages will be included in the index. If empty,
        /// all sites are included.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        public AlgoliaIndex(Type type, string indexName,  DistinctOptions distinctOptions = null, IEnumerable<string> siteNames = null)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!typeof(AlgoliaSearchModel).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"The search model {type} must extend {nameof(AlgoliaSearchModel)}.");
            }

            Type = type;
            IndexName = indexName;
            DistinctOptions = distinctOptions;
            SiteNames = siteNames ?? Enumerable.Empty<string>();
        }
    }
}
