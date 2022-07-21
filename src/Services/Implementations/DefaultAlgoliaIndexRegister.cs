using Kentico.Xperience.AlgoliaSearch.Models;

using System;
using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAlgoliaIndexRegister"/>.
    /// </summary>
    public class DefaultAlgoliaIndexRegister : IAlgoliaIndexRegister
    {
        private readonly Stack<AlgoliaIndex> indexes = new Stack<AlgoliaIndex>();


        public IAlgoliaIndexRegister Add<TModel>(string indexName, IEnumerable<string> siteNames = null) where TModel : AlgoliaSearchModel
        {
            indexes.Push(new AlgoliaIndex
            {
                IndexName = indexName,
                Type = typeof(TModel),
                SiteNames = siteNames
            });

            return this;
        }


        public AlgoliaIndex Pop()
        {
            try
            {
                return indexes.Pop();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
