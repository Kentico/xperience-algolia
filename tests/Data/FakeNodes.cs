using CMS.DataEngine;
using CMS.DocumentEngine;

using System;
using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class FakeNodes
    {
        private static Dictionary<string, TreeNode> nodes = new Dictionary<string, TreeNode>();


        public static string DOCTYPE_ARTICLE = "Test.Article";
        public static string DOCTYPE_PRODUCT = "Test.Product";


        public static void MakeNode(string nodeAliasPath, string pageType, string culture = "en-US")
        {
            var node = TreeNode.New(pageType).With(p =>
            {
                p.DocumentName = Guid.NewGuid().ToString();
                p.DocumentCulture = culture;
                p.SetValue("ArticleText", Guid.NewGuid().ToString());
                p.SetValue("NodeAliasPath", nodeAliasPath);
            });

            nodes.Add(nodeAliasPath, node);
        }


        public static TreeNode GetNode(string nodeAliasPath)
        {
            return nodes[nodeAliasPath];
        }
    }
}
