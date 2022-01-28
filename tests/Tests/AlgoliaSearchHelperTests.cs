using Algolia.Search.Clients;

using CMS.Core;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Helpers;
using Kentico.Xperience.AlgoliaSearch.Models;
using Kentico.Xperience.AlgoliaSearch.Models.Facets;
using Kentico.Xperience.AlgoliaSearch.Test;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Tests
{
    [TestFixture]
    internal class AlgoliaSearchHelperTests
    {
        internal class GetAlgoliaOptionsTests : AlgoliaTest
        {
            [Test]
            public void GetAlgoliaOptions_ReturnsAlgoliaOptions()
            {
                var configuration = Service.Resolve<IConfiguration>();
                var algoliaOptions = AlgoliaSearchHelper.GetAlgoliaOptions(configuration);

                Assert.Multiple(() => {
                    Assert.IsInstanceOf<AlgoliaOptions>(algoliaOptions);
                    Assert.AreEqual(APPLICATION_ID, algoliaOptions.ApplicationId);
                    Assert.AreEqual(API_KEY, algoliaOptions.ApiKey);
                });
            }
        }


        internal class GetSearchClientTests : AlgoliaTest
        {
            [Test]
            public void GetSearchClient_WithConfiguration_ReturnsClient()
            {
                var configuration = Service.Resolve<IConfiguration>();
                var client = AlgoliaSearchHelper.GetSearchClient(configuration);

                Assert.IsInstanceOf<SearchClient>(client);
            }
        }


        internal class GetFacetedAttributesTests : AlgoliaTest
        {
            private Dictionary<string, Dictionary<string, long>> facetsFromResponse = new Dictionary<string, Dictionary<string, long>>()
            {
                {
                    "attr1",
                    new Dictionary<string, long>() { { "facet1", 1 } }
                },
                {
                    "attr2",
                    new Dictionary<string, long>() { { "facet2", 2 }, { "facet3", 3 } }
                }
            };


            private AlgoliaFacetedAttribute[] facetsFromFilter = new AlgoliaFacetedAttribute[] {
                new AlgoliaFacetedAttribute() {
                    Attribute = "attr1",
                    Facets = new AlgoliaFacet[] {
                        new AlgoliaFacet() { Attribute = "attr1", Count = 0, Value = "facet0" },
                        new AlgoliaFacet() { Attribute = "attr1", Count = 1, Value = "facet1", IsChecked = true }
                    }
                },
                new AlgoliaFacetedAttribute() {
                    Attribute = "attr2",
                    Facets = new AlgoliaFacet[] {
                        new AlgoliaFacet() { Attribute = "attr2", Count = 2, Value = "facet2" },
                        new AlgoliaFacet() { Attribute = "attr2", Count = 3, Value = "facet3", IsChecked = true }
                    }
                }
            };


            [Test]
            public void GetFacetedAttributes_NewFilter_ReturnsNewFacets()
            {
                var filter = new AlgoliaFacetFilterViewModel();
                var facets = AlgoliaSearchHelper.GetFacetedAttributes(facetsFromResponse, filter);

                Assert.Multiple(() => {
                    Assert.True(facets.Any(attr => attr.Attribute == "attr1"));
                    Assert.True(facets.Any(attr => attr.Attribute == "attr2"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Any(facet => facet.Value == "facet1"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Any(facet => facet.Value == "facet2"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Any(facet => facet.Value == "facet3"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Where(facet => facet.Value == "facet1").FirstOrDefault().Count == 1);
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Where(facet => facet.Value == "facet2").FirstOrDefault().Count == 2);
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Where(facet => facet.Value == "facet3").FirstOrDefault().Count == 3);
                });
            }


            [Test]
            public void GetFacetedAttributes_ExistingFilter_ReturnsEmptyFacets()
            {
                var filter = new AlgoliaFacetFilterViewModel(facetsFromFilter);
                var facets = AlgoliaSearchHelper.GetFacetedAttributes(facetsFromResponse, filter);

                Assert.Multiple(() => {
                    Assert.True(facets.Any(attr => attr.Attribute == "attr1"));
                    Assert.True(facets.Any(attr => attr.Attribute == "attr2"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Any(facet => facet.Value == "facet0"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Any(facet => facet.Value == "facet1"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Any(facet => facet.Value == "facet2"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Any(facet => facet.Value == "facet3"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Where(facet => facet.Value == "facet0").FirstOrDefault().Count == 0);
                    Assert.True(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Where(facet => facet.Value == "facet1").FirstOrDefault().Count == 1);
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Where(facet => facet.Value == "facet2").FirstOrDefault().Count == 2);
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Where(facet => facet.Value == "facet3").FirstOrDefault().Count == 3);
                });
            }


            [Test]
            public void GetFacetedAttributes_ExistingFilter_ExcludeEmptyFacets_DoesntContainEmptyFacet()
            {
                var filter = new AlgoliaFacetFilterViewModel(facetsFromFilter);
                var facets = AlgoliaSearchHelper.GetFacetedAttributes(facetsFromResponse, filter, false);

                Assert.Multiple(() => {
                    Assert.True(facets.Any(attr => attr.Attribute == "attr1"));
                    Assert.True(facets.Any(attr => attr.Attribute == "attr2"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Any(facet => facet.Value == "facet1"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Any(facet => facet.Value == "facet2"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Any(facet => facet.Value == "facet3"));
                    Assert.True(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Where(facet => facet.Value == "facet1").FirstOrDefault().Count == 1);
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Where(facet => facet.Value == "facet2").FirstOrDefault().Count == 2);
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Where(facet => facet.Value == "facet3").FirstOrDefault().Count == 3);

                    Assert.False(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Any(facet => facet.Value == "facet0"));
                });
            }


            [Test]
            public void GetFacetedAttributes_ExistingFilter_RetainsCheckedState()
            {
                var filter = new AlgoliaFacetFilterViewModel(facetsFromFilter);
                var facets = AlgoliaSearchHelper.GetFacetedAttributes(facetsFromResponse, filter);

                Assert.Multiple(() => {
                    Assert.True(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Where(facet => facet.Value == "facet1").FirstOrDefault().IsChecked);
                    Assert.True(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Where(facet => facet.Value == "facet3").FirstOrDefault().IsChecked);

                    Assert.False(facets.Where(attr => attr.Attribute == "attr1").FirstOrDefault().Facets.Where(facet => facet.Value == "facet0").FirstOrDefault().IsChecked);
                    Assert.False(facets.Where(attr => attr.Attribute == "attr2").FirstOrDefault().Facets.Where(facet => facet.Value == "facet2").FirstOrDefault().IsChecked);
                });
            }
        }

        internal class GetFilterablePropertyNameTests : AlgoliaTest
        {
            [TestCase(typeof(Model1), nameof(Model1.DocumentCreatedWhen), ExpectedResult = "DocumentCreatedWhen")]
            [TestCase(typeof(Model2), nameof(Model2.Prop1), ExpectedResult = "filterOnly(Prop1)")]
            [TestCase(typeof(Model2), nameof(Model2.Prop2), ExpectedResult = "searchable(Prop2)")]
            public string GetFilterablePropertyName_ConvertedToAlgoliaFormat(Type searchModelType, string propertyName)
            {
                var property = searchModelType.GetProperty(propertyName);

                return AlgoliaSearchHelper.GetFilterablePropertyName(property);
            }


            [Test]
            public void GetFilterablePropertyName_InvalidConfiguration_ThrowsInvalidOperationException()
            {
                var searchModelType = typeof(Model6);
                var property = searchModelType.GetProperty(nameof(Model6.Prop1));

                Assert.Throws<InvalidOperationException>(() => AlgoliaSearchHelper.GetFilterablePropertyName(property));
            }
        }


        internal class OrderSearchablePropertiesTests : AlgoliaTest
        {
            [TestCase(typeof(Model1), ExpectedResult = new string[] { "DocumentCreatedWhen" })]
            [TestCase(typeof(Model2), ExpectedResult = new string[] { "unordered(Prop1)", "Prop2" })]
            public string[] OrderSearchableProperties_ConvertedToAlgoliaFormat(Type searchModelType)
            {
                var searchableProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(SearchableAttribute)));

                return AlgoliaSearchHelper.OrderSearchableProperties(searchableProperties).ToArray();
            }


            [Test]
            public void OrderSearchableProperties_PropertiesWithSameOrder_AddedAsSingleString()
            {
                var searchModelType = typeof(Model3);
                var searchableProperties = searchModelType.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(SearchableAttribute)));
                var converted = AlgoliaSearchHelper.OrderSearchableProperties(searchableProperties);

                Assert.Multiple(() =>
                {
                    Assert.True(converted[0] == "Prop2,Prop3");
                    Assert.True(converted[1] == "Prop1");
                });
            }
        }
    }
}
