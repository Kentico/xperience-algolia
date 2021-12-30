[![Nuget](https://img.shields.io/nuget/v/Kentico.Xperience.AlgoliaSearch)](https://www.nuget.org/packages/Kentico.Xperience.AlgoliaSearch) ![Kentico.Xperience.Libraries 13.0.0](https://img.shields.io/badge/Kentico.Xperience.Libraries-v13.0.0-orange)

# Algolia Search Xperience Integration

This integration enables the creating of [Algolia](https://www.algolia.com/) search indexes and the indexing of Xperience content tree pages using a code-first approach. Developers can use the [.NET API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/csharp/?client=csharp), [JavaScript API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/javascript/?client=javascript), or [InstantSearch.js](https://www.algolia.com/doc/guides/building-search-ui/what-is-instantsearch/js/) to provide a search interface on their live site.

You can also check out the [Xperience Algolia Search Widgets](https://github.com/Kentico/xperience-algolia-widgets) repository for helpful Xperience page builder widgets.

## Installation

1. Install the [Kentico.Xperience.AlgoliaSearch](https://www.nuget.org/packages/Kentico.Xperience.AlgoliaSearch) NuGet package in both the CMS and .NET Core applications.
2. From the [Algolia dashboard](https://www.algolia.com/dashboard), open your application and click "API keys" to find your keys.
3. In your live-site project's `appsettings.json`, add the following section:

```json
"xperience.algolia": {
	"applicationId": "<your application ID>",
	"apiKey": "<your Admin API key>"
}
```

4. In your CMS project's `web.config` `appSettings` section, add the following keys:

```xml
<add key="AlgoliaApplicationId" value="<your application ID>"/>
<add key="AlgoliaApiKey" value="<your Admin API key>"/>
```

5. (Optional) Import the [Xperience Algolia module](#xperience-algolia-module) in your Xperience website.

## How it works

This integration uses a code-first approach to define Algolia indexes. A single class (created by your developers) contains the Algolia index fields, the individual field configurations, and automatically registers the Algolia index on application startup.

We recommend that your developers create a new [.NET Standard 2.0](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) Class Library project to contain the Algolia search models they will create. This project can be referenced by both the CMS and .NET Core projects, allowing developers to reference the stongly-typed search models in each application. As a result, your developers can utilize Algolia's [POCO philosophy](https://www.algolia.com/doc/api-client/getting-started/install/csharp/?client=csharp#poco-types-and-jsonnet) while creating the search interface.

## Creating and registering an Algolia index

An Algolia index and its fields are defined within a single class file, in which your custom class extends [`AlgoliaSearchModel`](https://github.com/Kentico/xperience-algolia/blob/master/src/Models/AlgoliaSearchModel.cs). Within the class, you define the fields of the index by creating properties which match the names of Xperience page fields to index. The Xperience fields available may come from the `TreeNode` object, `SKUTreeNode` for products, or any custom page type fields.

The index is registered via the [`RegisterAlgoliaIndex`](https://github.com/Kentico/xperience-algolia/blob/master/src/Attributes/RegisterAlgoliaIndexAttribute.cs) attribute which requires the type of the search model class and the code name of the Algolia index:

```cs
using Kentico.Xperience.AlgoliaSearch.Models;
using System;

[assembly: RegisterAlgoliaIndex(typeof(AlgoliaSiteSearchModel), AlgoliaSiteSearchModel.IndexName)]
namespace DancingGoat
{
    public class AlgoliaSiteSearchModel : AlgoliaSearchModel
    {
        public const string IndexName = "AlgoliaSiteIndex";

        public string DocumentName { get; set; }

        public decimal? SKUPrice { get; set; }

        public string ArticleText { get; set; }
    }
}
```

### Determining the pages to index

While the above sample code will create an Algolia index, pages in the content tree will not be indexed until one or more [`IncludedPathAttribute`](https://github.com/Kentico/xperience-algolia/blob/master/src/Attributes/IncludedPathAttribute.cs) attributes are applied to the class. The `IncludedPathAttribute` has three properties to configure:

- __AliasPath__: The path of the content tree to index. Use "/%" to index all children of a page.
- __PageTypes__ (optional): The code names of the page types under the specified `AliasPath` to index. If not provided, all page types are indexed.
- __Cultures__ (optional): The culture codes of the page language versions to include in the index. If not provided, all culture versions are indexed.

We recommend using the generated [Xperience page type code](https://docs.xperience.io/developing-websites/generating-classes-for-xperience-objects) to reference page type class names. Below is an example of an Algolia index which includes multiple paths and page types:

```cs
using CMS.DocumentEngine.Types.DancingGoatCore;
using DancingGoat;
using Kentico.Xperience.AlgoliaSearch.Attributes;
using Kentico.Xperience.AlgoliaSearch.Models;
using System

[assembly: RegisterAlgoliaIndex(typeof(AlgoliaSiteSearchModel), AlgoliaSiteSearchModel.IndexName)]
namespace DancingGoat
{
    [IncludedPath("/Articles/%", new string[] { Article.CLASS_NAME })]
    [IncludedPath("/Store/%", new string[] { Brewer.CLASS_NAME, Coffee.CLASS_NAME })]
    public class AlgoliaSiteSearchModel : AlgoliaSearchModel
    {
        public const string IndexName = "AlgoliaSiteIndex";

        public string DocumentName { get; set; }

        public decimal? SKUPrice { get; set; }

        public string ArticleText { get; set; }
    }
}
```

## Configuring fields with attributes

This package includes five attributes which can be applied to each individual field to further configure the Algolia index:

- [__Searchable__](#searchable-attribute)
- [__Facetable__](#facetable-attribute)
- [__Retrievable__](#retrievable-attribute)
- [__Source__](#source-attribute)
- [__Url__](#url-attribute)

### Searchable attribute

This attribute indicates that a field is [searchable](https://www.algolia.com/doc/api-reference/api-parameters/searchableAttributes/#how-to-use). Optional attribute properties be defined to fine-tune the performance of your searchable attributes:

- __Order__ (optional): Fields with lower `Order` will be given priority when searching for text. Fields without `Order` set will be added to the end of the list (making them lower priority), while fields with the same `Order` will be added with the same priority and are automatically `Unordered`.
- __Unordered__ (optional): By default, matches at the beginning of an fields are more relevant than matches at the end of the text. If `true`, the position of the matched text in the field is irrelevant.

Usage:
```cs
[Searchable]
public string DocumentName { get; set; }

[Searchable(0)] // Highest priority
public string DocumentName { get; set; }

[Searchable(unordered: true)]
public string DocumentName { get; set; }
```

### Facetable attribute

This attribute indicates a field is a [facet or filter](https://www.algolia.com/doc/api-reference/api-parameters/attributesForFaceting/#how-to-use). By creating facets, your developers are able to create a [faceted search](https://www.algolia.com/doc/guides/managing-results/refine-results/faceting/) interface on the front-end application. Optional attribute properties can be defined to change the functionality of your faceted fields:

- __FilterOnly__ (optional): Defines the field as a filter and not a facet. If you do not need facets, defining a field as a filter reduces the size of the index and improves the speed of the search.

- __Searchable__ (optional): Allows developers to search for values within a facet, e.g. via the [`SearchForFacetValues()`](https://www.algolia.com/doc/api-reference/api-methods/search-for-facet-values/) method.

A field cannot be both `FilterOnly` and `Searchable`, or an exception will be thrown.

Usage:
```cs
[Facetable]
public decimal? SKUPrice { get; set; }

[Facetable(true)] // Filter
public decimal? SKUPrice { get; set; }

[Facetable(searchable: true)] // Searchable
public decimal? SKUPrice { get; set; }
```

### Retrievable attribute

This attribute determines which fields to [retrieve when searching](https://www.algolia.com/doc/api-reference/api-parameters/attributesToRetrieve/#how-to-use). Reducing the amount of fields retrieved will help improve the speed of your searches, without impacting the search functionality.

Usage:
```cs
[Searchable, Retrievable] // Used during searching and retrieved
public string DocumentName { get; set; }

[Searchable] // Used in searching but not retrieved
public string ArticleText { get; set; }
```

### Source attribute

This attribute can be used to alter the page field that the field value is retrieved from. This can be useful in indexes which include multiple page types, but the different page type fields should be stored in the same Algolia index field. For example, your index should contain a "thumbnail" field containing the URL to an image, but the image for each page type is stored in different page fields.

Columns specified in the `SourceAttribute` are parsed in the order they appear, until a non-empty string and non-null value is found, which is then indexed. We recommend referencing standard page fields and custom page type fields using `nameof()` to avoid typos.

Usage:
```cs
[Url, Retrievable]
[Source(nameof(SKUTreeNode.SKU.SKUImagePath), nameof(Article.ArticleTeaser))]
public string Thumbnail { get; set; }
```

### Url attribute

This attribute indicates that the value of the page field should be converted into an absolute live-site URL before indexing. This can be useful when configuring the [Display Preferences](https://www.algolia.com/doc/guides/managing-results/rules/merchandising-and-promoting/how-to/how-to-configure-and-use-the-visual-editor-with-category-pages/#configure-the-visual-editor) in Algolia, for example. This attribute can be used on a page type field which stores a URL as a relative URL (_~/getmedia_) or one that stores an Xperience attachment.

Usage:
```cs
[Url, Retrievable] // Attachment field
public string ArticleTeaser { get; set; }

[Url, Retrievable] // Multiple fields
[Source(nameof(SKUTreeNode.SKU.SKUImagePath), nameof(Article.ArticleTeaser))]
public string Thumbnail { get; set; }
```

## Xperience Algolia module

While the Xperience Algolia integration works without an Xperience interface, you may choose to import a custom module into your Xperience website to improve your user's experience. To do so, locate the latest `Kentico.Xperience.AlgoliaSearch` ZIP package in the root of this repository, download it, and [import it into your Xperience website](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects).

![Algolia module grid](/img/index-grid.png)

The newly-imported __Algolia search__ module will provide a listing of all registered Algolia search model code files, along with some statistics directly from Algolia. By default, Algolia indexes are not rebuilt at any point- only updated and newly-created pages are indexed. To rebuild the index completely, use the circular arrow icon at the left of the grid.

To view details about an index, click the eye icon:

![Algolia index content](/img/index-content.png)

Switch to the __Search preview__ tab to perform a basic Algolia query:

![Algolia index preview](/img/index-preview.png)

This view will display the `objectID` and `className` fields, plus any other searchable field which contained the matching search term.