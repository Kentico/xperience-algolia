using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Test;

[assembly: RegisterAlgoliaIndex(typeof(SearchModel1), SearchModel1.IndexName)]
namespace Kentico.Xperience.AlgoliaSearch.Test
{
    public class SearchModel1 : AlgoliaSearchModel
    {
        public const string IndexName = "SearchModel1";


        [Searchable(unordered: true)]
        public string Prop6 { get; set; }


        [Searchable(4)]
        public string Prop5 { get; set; }


        [Searchable(1)]
        public string Prop1 {get;set;}


        [Searchable(1)]
        public string Prop2 { get; set; }


        [Searchable(2)]
        public string Prop3 { get; set; }


        [Searchable(3, true)]
        public string Prop4 { get; set; }
    }
}