﻿using System;

namespace Kentico.Xperience.AlgoliaSearch.Attributes
{
    /// <summary>
    /// A property attribute which specifies the column names that are used
    /// as the data source for the property, in order of priority.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SourceAttribute : Attribute
    {
        /// <summary>
        /// A list of columns used to load an Algolia search model's property value
        /// from .Columns are checked for non-null values in the order they appear
        /// in the array.
        /// </summary>
        public string[] Sources
        {
            get;
            set;
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sources">A list of columns used to load an Algolia search
        /// model's property value from. Columns are checked for non-null values in
        /// the order they appear in the array.</param>
        public SourceAttribute(params string[] sources)
        {
            Sources = sources;
        }
    }
}