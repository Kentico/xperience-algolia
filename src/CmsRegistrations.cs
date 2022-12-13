using System.Runtime.CompilerServices;

using CMS;
using CMS.Core;

using Kentico.Xperience.Algolia;
using Kentico.Xperience.Algolia.Services;

[assembly: AssemblyDiscoverable]

// Allows the Algolia test project to read internal members
[assembly: InternalsVisibleTo("Kentico.Xperience.AlgoliaSearch.Tests")]

// Modules
[assembly: RegisterModule(typeof(AlgoliaSearchModule))]

// Default service implementations
[assembly: RegisterImplementation(typeof(IAlgoliaClient), typeof(DefaultAlgoliaClient), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(IAlgoliaIndexService), typeof(DefaultAlgoliaIndexService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(IAlgoliaInsightsService), typeof(DefaultAlgoliaInsightsService), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(IAlgoliaObjectGenerator), typeof(DefaultAlgoliaObjectGenerator), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
[assembly: RegisterImplementation(typeof(IAlgoliaTaskLogger), typeof(DefaultAlgoliaTaskLogger), Lifestyle = Lifestyle.Singleton, Priority = RegistrationPriority.SystemDefault)]
