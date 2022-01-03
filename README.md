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

## Searching the index

You can use Algolia's [.NET API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/csharp/?client=csharp), [JavaScript API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/javascript/?client=javascript), or [InstantSearch.js](https://www.algolia.com/doc/guides/building-search-ui/what-is-instantsearch/js/) to develop a search interface on your live site. If you are developing the search functionality using .NET, you can use [`AlgoliaSearchHelper.GetSearchIndex()`](https://github.com/Kentico/xperience-algolia/blob/master/src/AlgoliaSearchHelper.cs#L130) to get the [`SearchIndex`](algolia.com/doc/api-reference/api-methods/search/?client=csharp) object based on your index's code name:

```cs
public ActionResult Search(string searchText, int page = DEFAULT_PAGE_NUMBER)
{
    page = Math.Max(page, DEFAULT_PAGE_NUMBER);

    var searchIndex = AlgoliaSearchHelper.GetSearchIndex(AlgoliaSiteSearchModel.IndexName);
    var query = new Query(searchText)
    {
        Page = page,
        HitsPerPage = PAGE_SIZE
    };

    var results = searchIndex.Search<AlgoliaSiteSearchModel>(query);
    ...
```

The `Hits` object of the [search response](https://www.algolia.com/doc/api-reference/api-methods/search/?client=csharp#response) will be a list of the strongly typed objects defined by your search model (`AlgoliaSiteSearchModel` in the above example). Other helpful properties of the results are `NbPages` and `NbHits`.

The properties of each hit will be populated from the Algolia index, but be sure to check for `null` values! For example, a property that does _not_ have the [`Retrievable`](#retrievable-attribute) attribute will not be returned, and custom page type fields will only be present for results of that type. That is, a property named "ArticleText" will most likely be `null` for products on your site. You can reference the [`AlgoliaSearchModel.ClassName`](https://github.com/Kentico/xperience-algolia/blob/master/src/Models/AlgoliaSearchModel.cs#L27) property present on all indexes to check the type of the returned hit.

Once the search is performed, pass the `Hits` and paging information to your view:

```cs
return View(new SearchResultsModel()
{
    Items = results.Hits,
    Query = searchText,
    CurrentPage = page,
    NumberOfPages = results.NbPages
});
```

In the view, loop through the `Hits` and display the results using a [display template](https://docs.microsoft.com/en-us/dotnet/api/system.web.mvc.html.displayextensions.displayfor?view=aspnet-mvc-5.2#System_Web_Mvc_Html_DisplayExtensions_DisplayFor__2_System_Web_Mvc_HtmlHelper___0__System_Linq_Expressions_Expression_System_Func___0___1___System_String_System_String_). You can define separate display templates for products or each page type if you'd like:

```cs
foreach (var item in Model.Items)
{
    if (item.SKUPrice != null)
    {
        @Html.DisplayFor(m => item, "SiteSearchProductResult")
    }
    else if (item.ClassName == Article.CLASS_NAME)
    {
        @Html.DisplayFor(m => item, "SiteSearchArticleResult")
    }
    else
    {
        @Html.DisplayFor(m => item, "SiteSearchResult")
    }
}
```

In the display template, reference your search model's properties to display the result:

```cs
@model DancingGoat.AlgoliaSiteSearchModel

<div class="row search-tile">
    <div class="col-md-4 col-lg-3">
        @if (!string.IsNullOrEmpty(Model.Thumbnail))
        {
            <a href="@Model.Url" title="@Model.DocumentName">
                <img src="@Model.Thumbnail" alt="@Model.DocumentName" title="@Model.DocumentName" class="img-responsive" />
            </a>
        }
    </div>
    <div class="col-md-8 col-lg-9 search-tile-content">
        <h3 class="h4 search-tile-title">
            <a href="@Model.Url">@Model.DocumentName</a>
        </h3>
        <div class="search-tile-badge">@Model.ClassName</div>
        <div class="search-tile-subtitle">@Model.DocumentCreatedWhen.ToShortDateString()</div>
        <div>@Html.Raw(Model.ShortDescription)</div>
    </div>
</div>
```

### Creating an autocomplete search box

Algolia provides [autocomplete](https://www.algolia.com/doc/ui-libraries/autocomplete/introduction/what-is-autocomplete/) functionality via javascript which you can [install](https://www.algolia.com/doc/ui-libraries/autocomplete/introduction/getting-started/#installation) and set up any way you'd like. Below is an example of how we added autocomplete functionality to the Dancing Goat demo site's main search box in the top-right of every page.

1. In the `_Layout.cshtml` view which is rendered for every page, add a reference to Algolia's scripts and the default theme for autocomplete:

```html
<script src="//cdn.jsdelivr.net/algoliasearch/3/algoliasearch.min.js"></script>
<script src="//cdn.jsdelivr.net/autocomplete.js/0/autocomplete.min.js"></script>
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@@algolia/autocomplete-theme-classic"/>
```

2. Remove the existing search form and add a container for the autocomplete search box:

```html
<li class="search-menu-item">
    <div class="searchBox">
        <input id="search-input" placeholder="Search">
    </div>
</li>
```

3. From the [Algolia dashboard](https://www.algolia.com/dashboard), open your application and click "API keys" to find your keys.
4. Still in `_Layout.cshtml`, add javascript to the `<head>` which loads your Algolia index. Be sure to use your __Search API Key__ which is public, and _not_ your __Admin API Key__!

```html
<script type="text/javascript">
    var client = algoliasearch('<your Application ID>', '<your Search API Key>');
    var index = client.initIndex('@AlgoliaSiteSearchModel.IndexName');
</script>
```

> :bulb: You can also load the Application ID and Search API Key from the `appsettings.json` file by injecting `IConfiguration` into your view.

5. Initialize the autocomplete search box, then create a handler for when users click on autocomplete suggestions, and when the _Enter_ button is pushed:

```js
var autocompleteBox = autocomplete('#search-input', {hint: false}, [
{
    source: autocomplete.sources.hits(index, {hitsPerPage: 5}),
    displayKey: 'documentName' // The Algolia field used to display the title of a suggestion
}
]).on('autocomplete:selected', function(event, suggestion, dataset) {
	window.location = suggestion.url; // Navigate to the clicked suggestion
});

document.querySelector("#search-input").addEventListener("keyup", (e) => {
	if (e.key === 'Enter') {
        // Navigate to search results page when Enter is pressed
        var searchText = document.querySelector("#search-input").value;
        window.location = '@(Url.Action("Index", "Search"))?searchtext=' + searchText;
    }
});
```

When you run the Dancing Goat website and start typing into the search box, records from the Algolia index will be suggested:

![Autocomplete default theme](/img/autocomplete-default-theme.png)

### Customizing the autocomplete search box

In our sample implementation of the Algolia autocomplete search box we used the standard [Autocomplete classic theme](https://www.algolia.com/doc/ui-libraries/autocomplete/introduction/getting-started/#install-the-autocomplete-classic-theme) for basic styling of the search box and the autocomplete suggestion layout. You can reference the theme's [CSS classes and variables](https://www.algolia.com/doc/ui-libraries/autocomplete/api-reference/autocomplete-theme-classic/) to customize the appearance of the search box to match the design of your website.

In the Dancing Goat website, we added the following to the CSS which styles the search box and suggestions to match the Dancing Goat theme:

```css
/*# Algolia search box #*/
.searchBox .aa-dropdown-menu {
    background-color: #fff;
    padding: 5px;
    top: 120% !important;
    width: 100%;
    box-shadow: 0 1px 0 0 rgba(0, 0, 0, 0.2), 0 2px 3px 0 rgba(0, 0, 0, 0.1);
}
.searchBox .algolia-autocomplete {
    width: 100%;
}

.searchBox .aa-input {
    width: 100%;
    background-color: transparent;
    padding-left: 10px;
    padding-top: 5px;
}

.searchBox .aa-suggestion {
    padding: 5px 5px 0;
}

.searchBox .aa-suggestion em {
    color: #4098ce;
}

.searchBox .aa-suggestion.aa-cursor {
    background: #eee;
    cursor: pointer;
}
```

The layout of each individual suggestion can be customized by providing a [custom template](https://www.algolia.com/doc/ui-libraries/autocomplete/core-concepts/templates/) in the `autocomplete()` function. In the Dancing Goat website, we can add an image to each suggestion and highlight the matching search term by adding the following to our javascript:


```js
var autocompleteBox = autocomplete('#search-input', {hint: false}, [
{
    source: autocomplete.sources.hits(index, {hitsPerPage: 5}),
    templates: {
        suggestion: (item) =>
            `<img style='width:40px;margin-right:10px' src='${item.thumbnail}'/><span>${item._highlightResult.documentName.value}</span>`
    }
}
```

> :warning: The fields `documentName` and `thumbnail` used in this example are not present in all Algolia indexes! If you follow this example, make sure you are using fields present in your index. See the [Source attribute](#source-attribute) to find out how the `thumbnail` field was defined.

This is the final result of adding our custom CSS and template:

![Autocomplete custom template](/img/autocomplete-custom-template.png)

## Xperience Algolia module

While the Xperience Algolia integration works without an Xperience interface, you may choose to import a custom module into your Xperience website to improve your user's experience. To do so, locate the latest _Kentico.Xperience.AlgoliaSearch_ ZIP package in the root of this repository, download it, and [import it into your Xperience website](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects).

![Algolia module grid](/img/index-grid.png)

The newly-imported __Algolia search__ module will provide a listing of all registered Algolia search model code files, along with some statistics directly from Algolia. By default, Algolia indexes are not rebuilt at any point- only updated and newly-created pages are indexed. To rebuild the index completely, use the circular arrow icon at the left of the grid.

To view details about an index, click the eye icon:

![Algolia index content](/img/index-content.png)

Switch to the __Search preview__ tab to perform a basic Algolia query:

![Algolia index preview](/img/index-preview.png)

This view will display the `objectID` and `className` fields, plus any other searchable field which contained the matching search term.