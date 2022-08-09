using System;

namespace Kentico.Xperience.AlgoliaSearch.Models
{
    /// <summary>
    /// Represents the distinct and de-duplication settings for the Algolia index.
    /// </summary>
    /// <remarks>See <see href="https://www.algolia.com/doc/guides/sending-and-managing-data/prepare-your-data/how-to/indexing-long-documents/"/>.</remarks>
    public class DistinctOptions
    {
        /// <summary>
        /// The name of the attribute used for Algolia de-duplication.
        /// </summary>
        public string DistinctAttribute
        {
            get;
        }


        /// <summary>
        /// The distinction level.
        /// </summary>
        public int DistinctLevel
        {
            get;
        }


        public DistinctOptions(string distinctAttribute, int distinctLevel)
        {
            if (String.IsNullOrEmpty(distinctAttribute))
            {
                throw new ArgumentNullException(nameof(distinctAttribute));
            }

            if (distinctLevel <= 0)
            {
                throw new InvalidOperationException("Distinct level must be greater than zero.");
            }

            DistinctAttribute = distinctAttribute;
            DistinctLevel = distinctLevel;
        }
    }
}
