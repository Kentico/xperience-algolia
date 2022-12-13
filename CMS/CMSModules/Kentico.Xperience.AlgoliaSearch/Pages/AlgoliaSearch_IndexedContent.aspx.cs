using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CMS.Helpers;

using Kentico.Xperience.Algolia.Attributes;
using Kentico.Xperience.Algolia.Models;

namespace Kentico.Xperience.Algolia.Pages
{
    public partial class AlgoliaSearch_IndexedContent : AlgoliaUIPage
    {
        private string indexName;
        private AlgoliaIndex algoliaIndex;


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            indexName = QueryHelper.GetString("indexName", "");
            if (indexName == null)
            {
                ShowError("Unable to load index name.");
                return;
            }

            var foundIndex = IndexStore.Instance.Get(indexName);
            if (foundIndex == null)
            {
                ShowError($"Error loading registered Algolia index '{indexName}.'");
                return;
            }

            ShowInformation($"The indexed columns and pages are defined in the class <b>{foundIndex.Type}</b>. To modify them, please contact your developer.");

            algoliaIndex = foundIndex;
            LoadProperties();
            LoadPaths();
        }


        private void LoadProperties()
        {
            var indexedProperties = new List<IndexedProperty>();
            var modelProperties = algoliaIndex.Type.GetProperties();

            foreach (var property in modelProperties)
            {
                var sources = String.Empty;
                if (Attribute.IsDefined(property, typeof(SourceAttribute)))
                {
                    var sourceAttribute = property.GetCustomAttribute<SourceAttribute>();
                    sources = sourceAttribute.Sources.Join(", ");
                }

                indexedProperties.Add(new IndexedProperty
                {
                    Name = property.Name,
                    Searchable = Attribute.IsDefined(property, typeof(SearchableAttribute)),
                    Retrievable = Attribute.IsDefined(property, typeof(RetrievableAttribute)),
                    Facetable = Attribute.IsDefined(property, typeof(FacetableAttribute)),
                    Source = sources
                });
            }

            ugProperties.DataSource = ToDataSet(indexedProperties);
        }


        private void LoadPaths()
        {
            var includedContent = new List<IncludedContent>();
            var includedPathAttributes = algoliaIndex.Type.GetCustomAttributes(typeof(IncludedPathAttribute), false);
            foreach (var includedPathAttribute in includedPathAttributes)
            {
                var includedPath = includedPathAttribute as IncludedPathAttribute;
                var pageTypes = includedPath.PageTypes.Any() ? String.Join(", ", includedPath.PageTypes) : "(all)";
                var cultures = includedPath.Cultures.Any() ? String.Join(", ", includedPath.Cultures) : "(all)";
                includedContent.Add(new IncludedContent
                {
                    Path = includedPath.AliasPath,
                    PageTypes = pageTypes,
                    Cultures = cultures
                });
            }

            ugIncludedContent.DataSource = ToDataSet(includedContent);
        }
    }
}