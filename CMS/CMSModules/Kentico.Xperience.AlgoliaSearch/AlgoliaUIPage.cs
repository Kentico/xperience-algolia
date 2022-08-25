using Kentico.Xperience.Algolia.KX13.Attributes;
using Kentico.Xperience.Algolia.KX13.Services;

using CMS.Core;
using CMS.UIControls;

using System;
using System.Collections.Generic;
using System.Data;

namespace Kentico.Xperience.Algolia.KX13
{
    /// <summary>
    /// Base class for Algolia custom module pages.
    /// </summary>
    public class AlgoliaUIPage : CMSPage
    {
        protected IAlgoliaRegistrationService algoliaRegistrationService;
        protected IAlgoliaSearchService algoliaSearchService;


        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            algoliaRegistrationService = Service.Resolve<IAlgoliaRegistrationService>();
            algoliaSearchService = Service.Resolve<IAlgoliaSearchService>();
        }


        /// <summary>
        /// Converts a collection of objects into a <see cref="DataSet"/>.
        /// </summary>
        protected DataSet ToDataSet<T>(IList<T> list)
        {
            Type elementType = typeof(T);
            DataSet ds = new DataSet();
            DataTable t = new DataTable();
            ds.Tables.Add(t);

            foreach (var propInfo in elementType.GetProperties())
            {
                Type ColType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;

                t.Columns.Add(propInfo.Name, ColType);
            }

            foreach (T item in list)
            {
                DataRow row = t.NewRow();

                foreach (var propInfo in elementType.GetProperties())
                {
                    row[propInfo.Name] = propInfo.GetValue(item, null) ?? DBNull.Value;
                }

                t.Rows.Add(row);
            }

            return ds;
        }


        /// <summary>
        /// Model for displaying a search model property configuration in a UniGrid.
        /// </summary>
        protected class IndexedProperty
        {
            /// <summary>
            /// The name of the property.
            /// </summary>
            public string Name
            {
                get;
                set;
            }


            /// <summary>
            /// True if the property uses the <see cref="SearchableAttribute"/>.
            /// </summary>
            public bool Searchable
            {
                get;
                set;
            }


            /// <summary>
            /// True if the property uses the <see cref="RetrievableAttribute"/>.
            /// </summary>
            public bool Retrievable
            {
                get;
                set;
            }


            /// <summary>
            /// True if the property uses the <see cref="FacetableAttribute"/>.
            /// </summary>
            public bool Facetable
            {
                get;
                set;
            }


            /// <summary>
            /// A list of column names compiled from the property's <see cref="SourceAttribute.Sources"/>.
            /// </summary>
            public string Source
            {
                get;
                set;
            }
        }


        /// <summary>
        /// Model for displaying a search model's included paths in a UniGrid.
        /// </summary>
        protected class IncludedContent
        {
            /// <summary>
            /// The NodeAliasPath included in the index.
            /// </summary>
            public string Path
            {
                get;
                set;
            }


            /// <summary>
            /// The page types included in the index.
            /// </summary>
            public string PageTypes
            {
                get;
                set;
            }


            /// <summary>
            /// The cultures included in the index.
            /// </summary>
            public string Cultures
            {
                get;
                set;
            }
        }
    }
}