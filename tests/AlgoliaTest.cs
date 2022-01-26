using CMS.Core;
using CMS.DocumentEngine;
using CMS.Tests;

using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Helpers;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Tests.DocumentEngine;

[assembly: Category("Algolia")]
namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class AlgoliaTest : UnitTests
    {
        [OneTimeSetUp, Category.Unit]
        public void OneTimeSetUp()
        {
            // Register Algolia indexes
            var attributes = GetAlgoliaIndexAttributes(Assembly.GetExecutingAssembly());
            foreach (var attribute in attributes)
            {
                AlgoliaRegistrationHelper.RegisterIndex(attribute.IndexName, attribute.Type);
            }

            // Register document types for faking
            DocumentGenerator.RegisterDocumentType<TreeNode>(FakeNodes.DOCTYPE_ARTICLE);
            DocumentGenerator.RegisterDocumentType<TreeNode>(FakeNodes.DOCTYPE_PRODUCT);
            Fake().DocumentType<TreeNode>(FakeNodes.DOCTYPE_ARTICLE);
            Fake().DocumentType<TreeNode>(FakeNodes.DOCTYPE_PRODUCT);

            FakeNodes.MakeNode("/Articles/1", FakeNodes.DOCTYPE_ARTICLE);
            FakeNodes.MakeNode("/CZ/Articles/1", FakeNodes.DOCTYPE_ARTICLE, "cs-CZ");
            FakeNodes.MakeNode("/Store/Products/1", FakeNodes.DOCTYPE_PRODUCT);
            FakeNodes.MakeNode("/CZ/Store/Products/2", FakeNodes.DOCTYPE_PRODUCT, "cs-CZ");
        }


        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AlgoliaRegistrationHelper.RegisteredIndexes.Clear();
            FakeNodes.ClearNodes();
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
