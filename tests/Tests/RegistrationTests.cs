using Kentico.Xperience.AlgoliaSearch.Helpers;

using NUnit.Framework;

using System;
using System.Linq;
using System.Reflection;

using static Kentico.Xperience.AlgoliaSearch.Test.TestSearchModels;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    [TestFixture]
    internal class RegistrationTests : AlgoliaTest
    {
        [Test]
        public void AllIndexesRegistered()
        {
            var actualRegistrations = AlgoliaSearchModule.GetAlgoliaIndexAttributes(Assembly.GetExecutingAssembly());
            Assert.AreEqual(actualRegistrations.Count(), AlgoliaSearchHelper.RegisteredIndexes.Count());
        }


        [Test]
        [TestCase(Model1.IndexName, ExpectedResult = typeof(Model1))]
        [TestCase(Model2.IndexName, ExpectedResult = typeof(Model2))]
        public Type SearchModelsRetrievable(string indexName)
        {
            return AlgoliaSearchHelper.GetModelByIndexName(indexName);
        }
    }
}
