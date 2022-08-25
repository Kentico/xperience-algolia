using System;

namespace Kentico.Xperience.Algolia.KX13.Attributes
{
    /// <summary>
    /// A property attribute to indicate a search model property is retrievable within Algolia.
    /// </summary>
    /// <remarks>See <see href="https://www.algolia.com/doc/api-reference/api-parameters/attributesToRetrieve/"/>.</remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RetrievableAttribute : Attribute
    {
    }
}