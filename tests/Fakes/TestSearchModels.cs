﻿using Kentico.Xperience.Algolia.Attributes;

namespace Kentico.Xperience.Algolia.Tests
{
    internal class TestSearchModels
    {
        [IncludedPath("/Articles/%", PageTypes = new string[] { FakeNodes.DOCTYPE_ARTICLE }, Cultures = new string[] { "en-US" })]
        public class ArticleEnSearchModel : BaseSearchModel
        {
            public string DocumentName { get; set; }


            [Facetable(Searchable = true)]
            public string FacetableProperty { get; set; }


            [Searchable(Unordered = true)]
            public string UnorderedProperty { get; set; }
        }


        [IncludedPath("/Products/%", PageTypes = new string[] { FakeNodes.DOCTYPE_PRODUCT })]
        public class ProductsSearchModel : BaseSearchModel
        {
            [Retrievable]
            public string RetrievableProperty { get; set; }


            [Searchable(Order = 1)]
            public string Order1Property1 { get; set; }


            [Searchable(Order = 1)]
            public string Order1Property2 { get; set; }


            [Searchable(Order = 2)]
            public string Order2Property { get; set; }
        }


        [IncludedPath("/Articles/%")]
        [IncludedPath("/Products/%")]
        public class SplittingModel : BaseSearchModel
        {
            [Searchable]
            public string AttributeForDistinct { get; set; }
        }


        public class InvalidFacetableModel : BaseSearchModel
        {
            [Facetable(FilterOnly = true, Searchable = true)]
            public string FacetableProperty { get; set; }
        }


        [IncludedPath("/Articles/%")]
        public class OtherSiteModel : BaseSearchModel
        {
        }
    }
}
