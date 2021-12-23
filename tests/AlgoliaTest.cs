using CMS.Core;
using CMS.Tests;

using Kentico.Xperience.AlgoliaSearch.Attributes;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class AlgoliaTest : UnitTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Register Algolia indexes
            var attributes = GetAlgoliaIndexAttributes(Assembly.GetExecutingAssembly());
            foreach (var attribute in attributes)
            {
                AlgoliaSearchHelper.RegisterIndex(attribute.IndexName, attribute.Type);
            }
        }


        private IEnumerable<RegisterAlgoliaIndexAttribute> GetAlgoliaIndexAttributes(Assembly assembly)
        {
            var attributes = Enumerable.Empty<RegisterAlgoliaIndexAttribute>();

            try
            {
                attributes = assembly.GetCustomAttributes(typeof(RegisterAlgoliaIndexAttribute), false)
                                    .Cast<RegisterAlgoliaIndexAttribute>();
            }
            catch (Exception exception)
            {
                var error = new DiscoveryError(exception, assembly.FullName);
                error.LogEvent();
            }

            return attributes;
        }
    }
}
