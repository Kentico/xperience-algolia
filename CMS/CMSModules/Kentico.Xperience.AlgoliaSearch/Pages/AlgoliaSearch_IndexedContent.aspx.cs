using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kentico.Xperience.AlgoliaSearch.Pages
{
    public partial class AlgoliaSearch_IndexedContent : AlgoliaUIPage
    {
        private string indexName;
        private Type searchModelType;


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            indexName = QueryHelper.GetString("indexName", "");
            if (indexName == null)
            {
                ShowError("Unable to load index name.");
                return;
            }

            var registerIndexAttribute = algoliaRegistrationService.RegisteredIndexes.FirstOrDefault(i => i.IndexName == indexName);
            if (registerIndexAttribute == null || registerIndexAttribute.Type == null)
            {
                ShowError("Unable to load index search model class.");
                return;
            }

            ShowInformation($"The indexed columns and pages are defined in the class <b>{registerIndexAttribute.Type}</b>. To modify them, please contact your developer.");

            LoadProperties();
            LoadPaths();
        }


        private void LoadProperties()
        {
            var indexedProperties = new List<IndexedProperty>();
            var modelProperties = searchModelType.GetProperties();

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
            var includedPathAttributes = searchModelType.GetCustomAttributes(typeof(IncludedPathAttribute), false);
            foreach (var includedPathAttribute in includedPathAttributes)
            {
                var includedPath = includedPathAttribute as IncludedPathAttribute;
                var pageTypes = (includedPath.PageTypes.Length == 0) ? "(all)" : String.Join(", ", includedPath.PageTypes);
                var cultures = (includedPath.Cultures.Length == 0) ? "(all)" : String.Join(", ", includedPath.Cultures);

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