using CMS.DocumentEngine;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    public class ModelBaseClass : AlgoliaSearchModel
    {
        [Searchable]
        public string DocumentName { get; set; }


        public override object OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
        {
            return ValidationHelper.GetString(foundValue, "").ToUpper();
        }
    }
}
