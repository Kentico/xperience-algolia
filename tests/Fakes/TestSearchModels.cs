using CMS.DocumentEngine;

using Kentico.Xperience.Algolia.KX13.Attributes;
using Kentico.Xperience.Algolia.KX13.Models;
using Kentico.Xperience.Algolia.KX13.Test;

using static Kentico.Xperience.Algolia.KX13.Test.TestSearchModels;

[assembly: RegisterAlgoliaIndex(typeof(Model1), Model1.IndexName)]
[assembly: RegisterAlgoliaIndex(typeof(Model2), Model2.IndexName, SiteNames = new string[] { AlgoliaTests.DEFAULT_SITE })]
[assembly: RegisterAlgoliaIndex(typeof(Model3), Model3.IndexName)]
[assembly: RegisterAlgoliaIndex(typeof(Model4), Model4.IndexName)]
[assembly: RegisterAlgoliaIndex(typeof(Model5), Model5.IndexName)]
[assembly: RegisterAlgoliaIndex(typeof(Model6), Model6.IndexName)]
[assembly: RegisterAlgoliaIndex(typeof(Model7), Model7.IndexName)]
namespace Kentico.Xperience.Algolia.KX13.Test
{
    public class TestSearchModels
    {
        [IncludedPath("/Articles/%")]
        public class Model1 : AlgoliaSearchModel
        {
            public const string IndexName = "Model1";


            [Searchable]
            [Facetable]
            [Retrievable]
            public string DocumentCreatedWhen { get; set; }
        }


        [IncludedPath("/%", PageTypes = new string[] { "Test.Article" })]
        public class Model2 : AlgoliaSearchModel
        {
            public const string IndexName = "Model2";


            [Facetable(FilterOnly = true)]
            [Searchable(Unordered = true)]
            [Source(new string[] { "Column1", "Column2" })]
            public string Prop1 { get; set; }


            [Facetable(Searchable = true)]
            [Searchable]
            public string Prop2 { get; set; }
        }


        [IncludedPath("/%", PageTypes = new string[] { "Test.Article" }, Cultures = new string[] { "en-US" })]
        public class Model3 : AlgoliaSearchModel
        {
            public const string IndexName = "Model3";


            [Searchable]
            public string Prop1 { get; set; }


            [Retrievable]
            [Searchable(Order = 0)]
            public string Prop2 { get; set; }


            [Retrievable]
            [Searchable(Order = 0)]
            public string Prop3 { get; set; }
        }


        [IncludedPath("/Store/Products/%")]
        public class Model4 : AlgoliaSearchModel
        {
            public const string IndexName = "Model4";


            [Source(new string[] { nameof(TreeNode.NodeAliasPath) })]
            public string Prop1 { get; set; }
        }


        public class Model5 : AlgoliaSearchModel
        {
            public const string IndexName = "Model5";


            [Searchable(Unordered = true)]
            public string Prop6 { get; set; }


            [Searchable(Order = 4)]
            public string Prop5 { get; set; }


            [Retrievable]
            [Searchable(Order = 1)]
            public string Prop1 { get; set; }


            [Retrievable]
            [Searchable(Order = 1)]
            public string Prop2 { get; set; }


            [Searchable(Order = 2)]
            public string Prop3 { get; set; }


            [Searchable(Order = 3, Unordered = true)]
            public string Prop4 { get; set; }
        }


        public class Model6 : AlgoliaSearchModel
        {
            public const string IndexName = "Model6";


            [Facetable(FilterOnly = true, Searchable = true)]
            [Searchable]
            public string Prop1 { get; set; }
        }


        public class Model7 : ModelBaseClass
        {
            public const string IndexName = "Model7";


            [Searchable]
            public string NodeAliasPath { get; set; }
        }


        [IncludedPath("/Articles/%")]
        public class Model8 : AlgoliaSearchModel
        {
            public const string IndexName = "Model8";


            [Searchable]
            public string DocumentCreatedWhen { get; set; }
        }
    }
}