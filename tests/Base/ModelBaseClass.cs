using CMS.DocumentEngine;
using CMS.Helpers;

using Kentico.Xperience.Algolia.KX13.Attributes;
using Kentico.Xperience.Algolia.KX13.Models;

namespace Kentico.Xperience.Algolia.KX13.Test
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
