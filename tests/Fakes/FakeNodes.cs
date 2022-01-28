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
                p.DocumentCulture = culture;
                p.SetValue("DocumentCreatedWhen", new DateTime(2022, 1, 1));
                p.SetValue("NodeAliasPath", nodeAliasPath);
            });

            nodes.Add(nodeAliasPath, node);
        }


        public static TreeNode GetNode(string nodeAliasPath)
        {
            return nodes[nodeAliasPath];
        }


        public static void ClearNodes()
        {
            nodes.Clear();
        }
    }
}
