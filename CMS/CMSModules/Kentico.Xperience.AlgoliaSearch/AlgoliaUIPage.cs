using CMS.UIControls;

using System;
using System.Collections.Generic;
using System.Data;

namespace Kentico.Xperience.AlgoliaSearch
{
    /// <summary>
    /// Base class for Algolia custom module pages.
    /// </summary>
    public class AlgoliaUIPage : CMSPage
    {
        /// <summary>
        /// Model for displaying a search model property configuration in a UniGrid.
        /// </summary>
        protected class IndexedProperty
        {
            public string Name { get; set; }


            public bool Searchable { get; set; }


            public bool Retrievable { get; set; }


            public bool Facetable { get; set;}
        }


        /// <summary>
        /// Model for displaying a search model's included paths in a UniGrid.
        /// </summary>
        protected class IncludedContent
        {
            public string Path { get; set; }


            public string PageTypes { get; set; }


            public string Cultures { get; set; }
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
    }
}