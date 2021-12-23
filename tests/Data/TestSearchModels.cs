using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;
using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

[assembly: RegisterAlgoliaIndex(typeof(Model1), Model1.IndexName)]
[assembly: RegisterAlgoliaIndex(typeof(Model2), Model2.IndexName)]
[assembly: RegisterAlgoliaIndex(typeof(Model3), Model3.IndexName)]
[assembly: RegisterAlgoliaIndex(typeof(Model4), Model4.IndexName)]
[assembly: RegisterAlgoliaIndex(typeof(Model5), Model5.IndexName)]
namespace Kentico.Xperience.AlgoliaSearch.Test
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
            public string Prop1 { get; set; }
        }


        [IncludedPath("/%")]
        public class Model2 : AlgoliaSearchModel
        {
            public const string IndexName = "Model2";


            [Facetable]
            [Searchable]
            public string Prop1 { get; set; }


            [Facetable]
            [Searchable]
            public string Prop2 { get; set; }
        }


        [IncludedPath("/%", new string[] { "Test.Article" }, new string[] { "en-US" })]
        public class Model3 : AlgoliaSearchModel
        {
            public const string IndexName = "Model3";


            [Searchable]
            public string Prop1 { get; set; }


            [Retrievable]
            [Searchable(0)]
            public string Prop2 { get; set; }


            [Retrievable]
            [Searchable(0)]
            public string Prop3 { get; set; }
        }


        public class Model4 : AlgoliaSearchModel
        {
            public const string IndexName = "Model4";


            public string Prop1 { get; set; }
        }


        public class Model5 : AlgoliaSearchModel
        {
            public const string IndexName = "Model5";


            [Searchable(unordered: true)]
            public string Prop6 { get; set; }


            [Searchable(4)]
            public string Prop5 { get; set; }


            [Retrievable]
            [Searchable(1)]
            public string Prop1 { get; set; }


            [Retrievable]
            [Searchable(1)]
            public string Prop2 { get; set; }


            [Searchable(2)]
            public string Prop3 { get; set; }


            [Searchable(3, true)]
            public string Prop4 { get; set; }
        }
    }
}