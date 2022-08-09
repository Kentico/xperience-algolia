using Kentico.Xperience.AlgoliaSearch.Models;

using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Contains a collection of <see cref="AlgoliaIndex"/> which are automatically registered by
    /// <see cref="IAlgoliaRegistrationService"/> during application startup.
    /// </summary>
    public interface IAlgoliaIndexRegister
    {
        /// <summary>
        /// Inserts an <see cref="AlgoliaIndex"/> into the register.
        /// </summary>
        /// <typeparam name="TModel">The search model class.</typeparam>
        /// <param name="indexName">The code name of the Algolia index.</param>
        /// <param name="siteNames">The code names of the sites whose pages will be included in the index.
        /// If empty, all sites are included.</param>
        /// <param name="distinctAttribute">The name of the attribute used for Algolia de-duplication.</param>
        /// <param name="distinctLevel">The distinction level.</param>
        /// <returns>The <see cref="IAlgoliaRegistrationService"/> for chaining.</returns>
        IAlgoliaIndexRegister Add<TModel>(string indexName, IEnumerable<string> siteNames = null, string distinctAttribute = null, int distinctLevel = 0) where TModel : AlgoliaSearchModel;


        /// <summary>
        /// Pops off the first <see cref="AlgoliaIndex"/> in the register, or <c>null</c> if the register
        /// is empty.
        /// </summary>
        AlgoliaIndex Pop();
    }
}
