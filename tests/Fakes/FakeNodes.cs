using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.SiteProvider;

using System;
using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal static class FakeNodes
    {
        private static readonly Dictionary<string, TreeNode> nodes = new Dictionary<string, TreeNode>();


        public static string DOCTYPE_ARTICLE = "Test.Article";
        public static string DOCTYPE_PRODUCT = "Test.Product";


        public static void MakeNode(string nodeAliasPath, string pageType, string culture = "en-US")
        {
            var site = SiteInfo.Provider.Get(AlgoliaTests.DEFAULT_SITE);
            var node = TreeNode.New(pageType).With(p =>
            {
                p.DocumentCulture = culture;
                p.SetValue("NodeSiteID", site.SiteID);
                p.SetValue("DocumentName", "name");
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
