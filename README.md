[![Nuget](https://img.shields.io/nuget/v/Kentico.Xperience.AlgoliaSearch)](https://www.nuget.org/packages/Kentico.Xperience.AlgoliaSearch) ![Kentico.Xperience.Libraries 13.0.0](https://img.shields.io/badge/Kentico.Xperience.Libraries-v13.0.0-orange)

# Algolia Search Xperience Integration

This integration enables the creating of [Algolia](https://www.algolia.com/) search indexes and the indexing of Xperience content tree pages using a code-first approach. Developers can use the [.NET API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/csharp/?client=csharp), [JavaScript API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/javascript/?client=javascript), or [InstantSearch.js](https://www.algolia.com/doc/guides/building-search-ui/what-is-instantsearch/js/) to provide a search interface on their live site.

## :rocket: Installation

1. Install the [Kentico.Xperience.AlgoliaSearch](https://www.nuget.org/packages/Kentico.Xperience.AlgoliaSearch) NuGet package in both the CMS and .NET Core applications.
2. From the [Algolia dashboard](https://www.algolia.com/dashboard), open your application and click "API keys" to find your keys.
3. In your live-site project's `appsettings.json`, add the following section:

```json
"xperience.algolia": {
	"applicationId": "<your application ID>",
	"apiKey": "<your Admin API key>",
    "searchKey": "<your Search API key>"
}
```

4. In your CMS project's `web.config` `appSettings` section, add the following keys:

```xml
<add key="AlgoliaApplicationId" value="<your application ID>"/>
<add key="AlgoliaApiKey" value="<your Admin API key>"/>
```

5. (Optional) Import the [Xperience Algolia module](#xperience-algolia-module) in your Xperience website.

## :computer: How it works

This integration uses a code-first approach to define Algolia indexes. A single class (created by your developers) contains the Algolia index attributes, the individual attribute configurations, and automatically registers the Algolia index on application startup.

We recommend that your developers create a new [.NET Standard 2.0](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) Class Library project to contain the Algolia search models they will create. This project can be referenced by both the CMS and .NET Core projects, allowing developers to reference the stongly-typed search models in each application. As a result, your developers can utilize Algolia's [POCO philosophy](https://www.algolia.com/doc/api-client/getting-started/install/csharp/?client=csharp#poco-types-and-jsonnet) while creating the search interface.

> :warning: When developing the .NET Standard Class Library project, remember to add `[assembly: CMS.AssemblyDiscoverable]` your code! See [Adding custom assemblies](https://docs.xperience.io/custom-development/adding-custom-assemblies).

## :gear: Creating and registering an Algolia index

An Algolia index and its attributes are defined within a single class file, in which your custom class extends [`AlgoliaSearchModel`](https://github.com/Kentico/xperience-algolia/blob/master/src/Models/AlgoliaSearchModel.cs). Within the class, you define the attributes of the index by creating properties which match the names of Xperience page fields to index. The Xperience fields available may come from the `TreeNode` object, `SKUTreeNode` for products, or any custom page type fields.

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

> :bulb: We recommend using the generated [Xperience page type code](https://docs.xperience.io/developing-websites/generating-classes-for-xperience-objects) to reference page type class names.

Below is an example of an Algolia index which includes multiple paths and page types:

```cs
[assembly: RegisterAlgoliaIndex(typeof(AlgoliaSiteSearchModel), AlgoliaSiteSearchModel.IndexName)]
namespace DancingGoat
{
    [IncludedPath("/Articles/%", new string[] { Article.CLASS_NAME })]
    [IncludedPath("/Store/%", new string[] { "DancingGoatCore.Brewer", "DancingGoatCore.Coffee", "DancingGoatCore.ElectricGrinder", "DancingGoatCore.FilterPack", "DancingGoatCore.ManualGrinder", "DancingGoatCore.Tableware" })]
    public class AlgoliaSiteSearchModel : AlgoliaSearchModel
    {
        public const string IndexName = nameof(AlgoliaSiteSearchModel);

        [Searchable, Retrievable]
        public string DocumentName { get; set; }

        [Url, Retrievable]
        [Source(nameof(SKUTreeNode.SKU.SKUImagePath), nameof(Article.ArticleTeaser))]
        public string Thumbnail { get; set; }

        [Searchable]
        [Source(nameof(SKUTreeNode.DocumentSKUDescription), nameof(Article.ArticleText))]
        public string Content { get; set; }

        [Searchable, Retrievable]
        [Source(nameof(SKUTreeNode.DocumentSKUShortDescription), nameof(Article.ArticleSummary))]
        public string ShortDescription { get; set; }

        [Facetable, Retrievable]
        public decimal? SKUPrice { get; set; }

        [Retrievable]
        public int SKUPublicStatusID { get; set; }

        [Retrievable]
        public int SKUID { get; set; }

        [Retrievable]
        public DateTime DocumentCreatedWhen { get; set; }

        [Facetable]
        public string CoffeeProcessing { get; set; }

        [Facetable]
        public bool CoffeeIsDecaf { get; set; }
    }
```

## :memo: Configuring Algolia attributes

This package includes five attributes which can be applied to each individual Algolia attribute to further configure the Algolia index:

- [__Searchable__](#searchable-attribute)
- [__Facetable__](#facetable-attribute)
- [__Retrievable__](#retrievable-attribute)
- [__Source__](#source-attribute)
- [__Url__](#url-attribute)

### Searchable attribute

This attribute indicates that an Algolia attribute is [searchable](https://www.algolia.com/doc/api-reference/api-parameters/searchableAttributes/#how-to-use). Optional attribute properties be defined to fine-tune the performance of your searchable attributes:

- __Order__ (optional): Attributes with lower `Order` will be given priority when searching for text. Attributes without `Order` set will be added to the end of the list (making them lower priority), while attributes with the same `Order` will be added with the same priority and are automatically `Unordered`.
- __Unordered__ (optional): By default, matches at the beginning of an attribute are more relevant than matches at the end of the text. If `true`, the position of the matched text in the attribute is irrelevant.

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

This attribute indicates an Algolia attribute is a [facet or filter](https://www.algolia.com/doc/api-reference/api-parameters/attributesForFaceting/#how-to-use). By creating facets, your developers are able to create a [faceted search](https://www.algolia.com/doc/guides/managing-results/refine-results/faceting/) interface on the front-end application. Optional attribute properties can be defined to change the functionality of your faceted attributes:

- __FilterOnly__ (optional): Defines the attribute as a filter and not a facet. If you do not need facets, defining a attribute as a filter reduces the size of the index and improves the speed of the search.

- __Searchable__ (optional): Allows developers to search for values within a facet, e.g. via the [`SearchForFacetValues()`](https://www.algolia.com/doc/api-reference/api-methods/search-for-facet-values/) method.

> :warning: An attribute cannot be both `FilterOnly` and `Searchable`, or an exception will be thrown.

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

This attribute determines which attributes to [retrieve when searching](https://www.algolia.com/doc/api-reference/api-parameters/attributesToRetrieve/#how-to-use). Reducing the amount of attributes retrieved will help improve the speed of your searches, without impacting the search functionality.

Usage:
```cs
[Searchable, Retrievable] // Used during searching and retrieved
public string DocumentName { get; set; }

[Searchable] // Used in searching but not retrieved
public string ArticleText { get; set; }
```

### Source attribute

This attribute can be used to alter the page field that the attribute value is retrieved from. This can be useful in indexes which include multiple page types, but the different page type fields should be stored in the same Algolia attribute. For example, your index should contain a "Thumbnail" attribute containing the URL to an image, but the image for each page type is stored in different page fields.

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

## :mag_right: Searching the index

You can use Algolia's [.NET API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/csharp/?client=csharp), [JavaScript API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/javascript/?client=javascript), or [InstantSearch.js](https://www.algolia.com/doc/guides/building-search-ui/what-is-instantsearch/js/) to develop a search interface on your live site. If you are developing the search functionality using .NET Core, you can use the `AddAlgolia()` extension method to inject `ISearchClient` as well as other classes into your Controllers/Views:

```cs
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddAlgolia(Configuration);
}
```

In your Controllers, you can get a `SearchIndex` object by calling `InitIndex()` on the search client using your index's code name. Then, construct a `Query` to search the Algolia index:

```cs
private readonly ISearchClient _searchClient;

public SearchController(ISearchClient searchClient)
{
    _searchClient = searchClient;
}

public ActionResult Search(string searchText, int page = DEFAULT_PAGE_NUMBER)
{
    page = Math.Max(page, DEFAULT_PAGE_NUMBER);

    var searchIndex = _searchClient.InitIndex(AlgoliaSiteSearchModel.IndexName);
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

```cshtml
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

```cshtml
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

1. In the _\_Layout.cshtml_ view which is rendered for every page, add a reference to Algolia's scripts and the default theme for autocomplete:

```cshtml
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

3. Load the Algolia keys from `appsettings.json`:

```cshtml
@inject IConfiguration configuration

@{
    var algoliaOptions = AlgoliaSearchHelper.GetAlgoliaOptions(configuration);
}
```

4. Add javascript near the end of the `<body>` which loads your Algolia index. Be sure to use your __Search API Key__ which is public, and _not_ your __Admin API Key__!

```js
<script type="text/javascript">
    var client = algoliasearch('@algoliaOptions.ApplicationId', '@algoliaOptions.SearchKey');
    var index = client.initIndex('@AlgoliaSiteSearchModel.IndexName');
</script>
```

> :bulb: You can also load the Application ID and Search API Key from the `appsettings.json` file by injecting `IConfiguration` into your view.

5. Initialize the autocomplete search box, then create a handler for when users click on autocomplete suggestions, and when the _Enter_ button is pushed:

```js
var autocompleteBox = autocomplete('#search-input', {hint: false}, [
{
    source: autocomplete.sources.hits(index, {hitsPerPage: 5}),
    displayKey: 'DocumentName' // The Algolia attribute used to display the title of a suggestion
}
]).on('autocomplete:selected', function(event, suggestion, dataset) {
	window.location = suggestion.Url; // Navigate to the clicked suggestion
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
            `<img style='width:40px;margin-right:10px' src='${item.Thumbnail}'/><span>${item._highlightResult.DocumentName.value}</span>`
    }
}
```

> :warning: The attributes `DocumentName` and `Thumbnail` used in this example are not present in all Algolia indexes! If you follow this example, make sure you are using attributes present in your index. See the [sample search model](#determining-the-pages-to-index) to find out how these attributes were defined.

This is the final result of adding our custom CSS and template:

![Autocomplete custom template](/img/autocomplete-custom-template.png)

## :ballot_box_with_check: Faceted search

As the search interface can be designed in multiple languages using Algolia's APIs, your developers can implement [faceted search](https://www.algolia.com/doc/guides/managing-results/refine-results/faceting/) in any way they'd like. However, this repository contains some helpful classes to develop faceted search using C#. The following is an example of creating a faceted search interface within the Dancing Goat sample site's store.

### Setting up basic search

The Dancing Goat store doesn't use search out-of-the-box, so first we need to hook it up to Algolia. In this example, we will be using the search model seen in [Determining the pages to index](#determining-the-pages-to-index).

1. Inject an instance of `SearchClient` into the `CoffeesController` as described in [this section](#mag_right-searching-the-index).

2. In __CoffeesController.cs__, create a method that will perform a standard Algolia search. In the `Query.Filters` property, add a filter to only retrieve records where `ClassName` is "DancingGoatCore.Coffee." We'll also specify which `Facets` we want to retrieve, but we're not using them yet.

```cs
private SearchResponse<AlgoliaSiteSearchModel> Search()
{
    var facetsToRetrieve = new string[] {
        nameof(AlgoliaSiteSearchModel.CoffeeIsDecaf),
        nameof(AlgoliaSiteSearchModel.CoffeeProcessing)
    };

    var query = new Query()
    {
        Filters = $"{nameof(AlgoliaSiteSearchModel.ClassName)}:{new Coffee().ClassName}",
        Facets = facetsToRetrieve
    };

    var searchIndex = _searchClient.InitIndex(AlgoliaSiteSearchModel.IndexName);
    return searchIndex.Search<AlgoliaSiteSearchModel>(query);
}
```

3. Create __AlgoliaStoreModel.cs__ which will represent a single product in the store listing:

```cs
using CMS.Core;
using CMS.Ecommerce;
using DancingGoat.Services;

namespace DancingGoat.Models.Store
{
    public class AlgoliaStoreModel
    {
        public AlgoliaSiteSearchModel Hit { get; set; }

        public ProductCatalogPrices PriceDetail { get; }

        public string PublicStatusName { get; set; }

        public bool IsInStock { get; }

        public bool AllowSale { get; }

        public bool Available
        {
            get
            {
                return IsInStock && AllowSale;
            }
        }

        public AlgoliaStoreModel(AlgoliaSiteSearchModel hit)
        {
            Hit = hit;
            var sku = SKUInfo.Provider.Get(hit.SKUID);
            if (sku.SKUPublicStatusID > 0)
            {
                PublicStatusName = PublicStatusInfo.Provider.Get(hit.SKUPublicStatusID).PublicStatusDisplayName;
            }

            var calc = Service.Resolve<ICalculationService>();
            PriceDetail = calc.CalculatePrice(sku);

            IsInStock = sku.SKUTrackInventory == TrackInventoryTypeEnum.Disabled ||
                        sku.SKUAvailableItems > 0;
            AllowSale = IsInStock || !sku.SKUSellOnlyAvailable;
        }
    }
}

```

4. In __ProductListViewModel.cs__, change the `Items` property to be a list of our new `AlgoliaStoreModel` items:

```cs
public IEnumerable<AlgoliaStoreModel> Items { get; set; }
```

5. Modify the `Index()` method to perform the search and provide the list of hits converted into `AlgoliaStoreModel` objects:

```cs
[HttpGet]
[HttpPost]
public ActionResult Index()
{
    var searchResponse = Search();
    var items = searchResponse.Hits.Select(
        hit => new AlgoliaStoreModel(hit)
    );

    var model = new ProductListViewModel
    {
        Items = items
    };

    return View(model);
}
```

6. Modify the views _Index.cshtml_, _CoffeeList.cshtml_, and _ProductListItem.cshtml_ to display your Algolia products.

### Filtering your search with facets

In the `Search()` method, we retrieved the _CoffeeIsDecaf_ and _CoffeeProcessing_ facets from Algolia, but they are not used yet. In the following steps we will use an `AlgoliaFacetFilterViewModel` (which implements `IAlgoliaFacetFilter`) to hold our facets and the current state of the faceted search interface.

This repository contains several classes which we can use to strongly-type the `SearchResponse.Facets` result of an AlgoliaSearch. The `AlgoliaSearchHelper.GetFacetedAttributes()` helps us convert the facet response into a list of `AlgoliaFacetedAttribute`s which contains the attribute name (e.g. "CoffeeIsDecaf"), localized display name (e.g. "Caffeine"), and a list of `AlgoliaFacet`s.

Each `AlgoliaFacet` represents the faceted attribute's possible values and contains the number of results that will be returned if the facet is enabled. For example, the "CoffeeProcessing" `AlgoliaFacetedAttribute` will contain 3 `AlgoliaFacet`s in its `Facets` property. The `Value` property of those facets will be "washed," "natural," and "semiwashed."

1. In the `Search()` method, add a parameter that accepts an `IAlgoliaFacetFilter` and adds a filter to `Query.FacetFilters` if facets are selected:

```cs
private SearchResponse<AlgoliaSiteSearchModel> Search(IAlgoliaFacetFilter filter = null)
{
    var facetsToRetrieve = new string[] {
        nameof(AlgoliaSiteSearchModel.CoffeeIsDecaf),
        nameof(AlgoliaSiteSearchModel.CoffeeProcessing)
    };

    var query = new Query()
    {
        Filters = $"{nameof(AlgoliaSiteSearchModel.ClassName)}:{new Coffee().ClassName}",
        Facets = facetsToRetrieve
    };

    if (filter != null)
    {
        query.FacetFilters = filter.GetFilters();
    }

    var searchIndex = _searchClient.InitIndex(AlgoliaSiteSearchModel.IndexName);
    return searchIndex.Search<AlgoliaSiteSearchModel>(query);
}
```

The `GetFilters()` method will return a facet filter for each facet in the `IAlgoliaFacetFilter` which has the `IsChecked` property set to true. For example, if a visitor on your store listing checked the boxes for decaf coffee with the "washed" processing type, the filter will look like this:

```js
[
    [ "CoffeeIsDecaf:true" ],
    [ "CoffeeProcessing:washed" ]
]
```

2. In `ProductListViewModel.cs`, add another property which will contain our facet filter:

```cs
public IAlgoliaFacetFilter AlgoliaFacetFilter { get; set; }
```

3. Modify the `Index()` action to accept an `AlgoliaFacetFilterViewModel`, pass it to the `Search()` method, parse the facets from the search response, then pass the filter to the view:

```cs
[HttpGet]
[HttpPost]
public ActionResult Index(AlgoliaFacetFilterViewModel filter)
{
    ModelState.Clear();

    var searchResponse = Search(filter);
    var items = searchResponse.Hits.Select(
        hit => new AlgoliaStoreModel(hit)
    );

    var facetedAttributes = AlgoliaSearchHelper.GetFacetedAttributes(searchResponse.Facets, filter);
    var filterViewModel = new AlgoliaFacetFilterViewModel(facetedAttributes);

    var model = new ProductListViewModel
    {
        Items = items,
        AlgoliaFacetFilter = filterViewModel
    };

    return View(model);
}
```

Here, the `GetFacetedAttributes()` method accepts the facets returned from Algolia, but also the current `IAlgoliaFacetFilter`. Because the entire list of available facets depends on the Algolia response, and the facets in your filter are replaced with new ones, this method ensures that a facet that was used previously (e.g. "CoffeeIsDecaf:true") maintains it's enabled state when reloading the search interface.

### Displaying the facets

If you've been following each section of this guide, the Dancing Goat store listing now uses Algolia search, and we have a filter which contains our Algolia facets and properly filters the search results. The final step is to display the facets in the store listing and handle user interaction with the facets.

1. In _Index.cshtml_, replace the existing filter with our own custom view and set the form action to "Index" as we will be reloading the entire layout:

```html
<aside class="col-md-4 col-lg-3 product-filter">
    <form asp-controller="Coffees" asp-action="Index">
        <partial name="~/Views/Shared/Algolia/_AlgoliaFacetFilter.cshtml" model="Model.AlgoliaFacetFilter" />
    </form>
</aside>
```

2. In the `Scripts` section of the view, remove the existing javascript and add a script that will post the form when a checkbox is toggled:

```html
<script>
    $(function () {
        $('.js-postback input:checkbox').change(function () {
            $(this).parents('form').submit();
        });
    });
</script>
```

3. Create the _/Views/Shared/Algolia/\_AlgoliaFacetFilter.cshtml_ view. As you can see in step 1, this view will accept our facet filter and should loop through each `AlogliaFacetedAttribute` it contains:

```cshtml
@using Kentico.Xperience.AlgoliaSearch.Models.Facets
@model AlgoliaFacetFilterViewModel

@for (var i=0; i<Model.FacetedAttributes.Count(); i++)
{
    @Html.EditorFor(model => Model.FacetedAttributes[i], "~/Views/Shared/Algolia/EditorTemplates/_AlgoliaFacetedAttribute.cshtml")
}
```

4. For each `AlgoliaFacetedAttribute` we now want to loop through each `AlgoliaFacet` it contains and display a checkbox that will enable the facet for filtering. Create the _/Views/Shared/Algolia/EditorTemplates/\_AlgoliaFacetedAttribute.cshtml_ file and render inputs for each facet:

```cshtml
@using Kentico.Xperience.AlgoliaSearch.Models.Facets
@model AlgoliaFacetedAttribute

<h4>@Model.DisplayName</h4>
@for (var i = 0; i < Model.Facets.Count(); i++)
{
    @Html.HiddenFor(m => Model.Facets[i].Value)
    @Html.HiddenFor(m => Model.Facets[i].Attribute)
    <span class="checkbox js-postback">
        <input asp-for="@Model.Facets[i].IsChecked" />
        <label asp-for="@Model.Facets[i].IsChecked">@Model.Facets[i].DisplayValue (@Model.Facets[i].Count)</label>
    </span>
}
```

We're done! Now, when you check one of the facets our javascript will cause the form to post back to the `Index()` action. The `filter` parameter will contain the facets that were displayed on the page, with the `IsChecked` property of each facet set accordingly. The filter is passed to our `Search()` method which uses `GetFilter()` to filter the search results, and a new `AlgoliaFacetFilterViewModel` is created with the results of the query.

![Dancing goat facet example](/img/dg-facets.png)

### Localizing facet names and values

Without localization, your view will display your facet attribute names (e.g. "CoffeeIsDecaf") instead of a human-readable header like "Caffeinated," and values like "true" and "false." You can use any localization approach you'd like, but the `IAlgoliaFacetFilter` contains a `Localize()` method that you can use out-of-the-box.

1. Inject `IStringLocalizer<SharedResources>` into the __CoffeeController__.
2. Call `filterViewModel.Localize()` in the `Index()` method after constructing the facet filter view model.

```cs
public ActionResult Index(AlgoliaFacetFilterViewModel filter)
{
    ...
    var facetedAttributes = AlgoliaSearchHelper.GetFacetedAttributes(searchResponse.Facets, filter);
    var filterViewModel = new AlgoliaFacetFilterViewModel(facetedAttributes);
    filterViewModel.Localize(_localizer);
    ...
}
```

3. The `Localize()` method searches for facet names with keys in the format _algolia.facet.[AttributeName]_, and facet values in the format _algolia.facet.[AttributeName].[FacetValue]_. In __SharedResources.resx__, add the following keys and your translations:

![Resource strings](/img/resource-strings.png)

4. Each `AlgoliaFacetedAttribute.DisplayName` and `AlgoliaFacet.DisplayValue` within the filter is now localized.

## :bulb: Personalizing search results

Algolia offers search result [Personalization](https://www.algolia.com/doc/guides/personalization/what-is-personalization/) to offer more relevant results to each individual visitor on your website. To begin personalizing search results, you first need to send [events](https://www.algolia.com/doc/guides/sending-events/planning/) to Algolia which detail the visitor's activity. As with much of the Algolia functionality, sending events is very flexible depending on your API of choice and how your search is implemented. You can choose to use any of the approaches in the Algolia documentation (e.g. [Google Tag Manager](https://www.algolia.com/doc/guides/sending-events/implementing/connectors/google-tag-manager/)). The following section details how to send events using C# with the assistance of some classes from this repository.

In order to begin using the Algolia Insights functionality detailed below, ensure that the `AddAlgolia()` extension method is called during startup. This allows you to inject the `IAlgoliaInsightsService` into your Controllers/Views to log events and conversions.

```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddAlgolia();
}
```

If you do not already have a basic search interface set up, check out [this section](#mag_right-searching-the-index) to set one up first.

### Sending search result click events/conversions

To track these types of events, the `ClickAnalytics` property must be enabled while creating your search query:

```cs
var query = new Query(searchText)
{
    Page = page,
    HitsPerPage = PAGE_SIZE,
    ClickAnalytics = true
};
```

This repository uses query string parameters to track the required data for submitting search result clicks and conversion to Algolia. As all search models extend `AlgoliaSearchModel` and contain a `Url` property, you can call `AlgoliaInsightsHelper.SetInsightsUrls()` to update the URL of your results with all the necessary data:

```cs
var results = searchIndex.Search<AlgoliaSiteSearchModel>(query);
AlgoliaInsightsHelper.SetInsightsUrls(results);
```

Now, when you display the search results using the `Url` property, it will look something like _https://mysite.com/store/brewers/aeropress/?object=88&pos=2&query=d057994ba21f0a56c75511c2c005f49f_. To submit the event to Algolia when your visitor clicks this link, inject an instance of `IAlgoliaInsightsService` into the view that renders the linked page. Or, you can inject it into the view which renders all pages, e.g. _\_Layout.cshtml_. Call `LogSearchResultClicked()`, `LogSearchResultConversion()`, or both methods of the service:

```cshtml
@inject IAlgoliaInsightsService _insightsService

@{
    _insightsService.LogSearchResultClicked("Search result clicked", AlgoliaSiteSearchModel.IndexName);
    _insightsService.LogSearchResultConversion("Search result converted", AlgoliaSiteSearchModel.IndexName);
}
```

When a visitor lands on a page after clicking a search result, these methods use the data contained in the query string to submit a search result click event or conversion. If the visitor arrives on the page without query string parameters (e.g. using the site navigation), nothing is logged.

### Sending generic page-related events/conversions

Aside from search result related events/conversions, there are many more generic events you may want to send to Algolia. For example, a very important conversion on E-commerce websites could be "Product added to cart." For sites that produce blog posts or articles, you may want to send an "Article viewed" event.

For a conversion, you can use the `IAlgoliaInsightsService.LogPageConversion()` method in your controllers or views. In the Dancing Goat sample site, we can log a "Product added to cart" conversion in the __CheckoutController__:

```cs
public ActionResult AddItem(CartItemUpdateModel item)
{
    if (ModelState.IsValid)
    {
        shoppingService.AddItemToCart(item.SKUID, item.Units);

        // Find the Xperience page related to the product
        var skuId = item.SKUID;
        var sku = SKUInfo.Provider.Get(item.SKUID);
        if (sku.IsProductVariant)
        {
            skuId = sku.SKUParentSKUID;
        }
        var currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
        var page = DocumentHelper.GetDocuments()
            .Culture(currentCulture)
            .WhereEquals(nameof(SKUTreeNode.NodeSKUID), skuId)
            .FirstOrDefault();

        // Log Algolia Insights conversion
        if (page != null)
        {
            _insightsService.LogPageConversion(page.DocumentID, "Product added to cart", AlgoliaSiteSearchModel.IndexName);
        }
        
    }

    return RedirectToAction("ShoppingCart");
}
```

We can also log an event when a visitor simply views a page with the `LogPageViewed()` method. For example, in the __ArticlesController__ we can log an "Article viewed" event:

```cs
public IActionResult Detail([FromServices] ArticleRepository articleRepository)
{
    var article = articleRepository.GetCurrent();
    _insightsService.LogPageViewed(article.DocumentID, "Article viewed", AlgoliaSiteSearchModel.IndexName);

    return new TemplateResult(article);
}
```

Or, in the _\_Details.cshtml_ view for products, we can log a "Product viewed" event:

```cshtml
@inject IAlgoliaInsightsService _insightsService
@inject IPageDataContextRetriever _pageDataContextRetriever

@{
    if(_pageDataContextRetriever.TryRetrieve<TreeNode>(out var context))
    {
        _insightsService.LogPageViewed(context.Page.DocumentID, "Product viewed", AlgoliaSiteSearchModel.IndexName);
    }
}
```

### Logging facet-related events/conversions

You can log events and conversions when facets are displayed to a visitor, or when they click on an individual facet. In this example, we will be using the code from our Dancing Goat faceted search example [here](#filtering-your-search-with-facets). Logging a "Search facets viewed" event can easily be done in the `Index()` action of __CoffeesController__. The `LogFacetsViewed()` method requires a list of `AlgoliaFacetedAttribute`s, which we already have from the `AlgoliaSearchHelper.GetFacetedAttributes()` call:

```cs
var searchResponse = Search(filter);
var facetedAttributes = AlgoliaSearchHelper.GetFacetedAttributes(searchResponse.Facets, filter);
_insightsService.LogFacetsViewed(facetedAttributes, "Store facets viewed", AlgoliaSiteSearchModel.IndexName);
```

To log an event or conversion when a facet is clicked, we need to use a little AJAX. First, in the _\_AlgoliaFacetedAttribute.cshtml_ view which displays each check box, add a `data` attribute that stores the facet name and value (e.g. "CoffeeIsDecaf:true"):

```cshtml
<input data-facet="@(Model.Attribute):@Model.Facets[i].Value" asp-for="@Model.Facets[i].IsChecked" />
```

In the _Index.cshtml_ view for the coffee listing, we already use the `change()` function to run some javascript when a facet is checked or unchecked. Let's add some code that runs only if the facet has been checked which gets the value of the new `data` attribute and sends a POST request:

```js
<script>
    $(function () {
        $('.js-postback input:checkbox').change(function () {
            if($(this).is(':checked')) {
                var facet = $(this).data('facet');
                fetch('@Url.Action("FacetClicked", "Store")?facet='+facet, {
                    method: 'POST'
                });
            }

            $(this).parents('form').submit();
        });
    });
</script>
```

This will send the request to the __StoreController__ `FacetClicked()` action, but you can send the request anywhere you'd like. Check the __Startup.cs__ to make sure your application can handle this request:

```cs
endpoints.MapControllerRoute(
    name: "facetClicked",
    pattern: "Algolia/FacetClicked/{facet?}",
    defaults: new { controller = "Store", action = "FacetClicked" }
);
```

Now, create the action in the appropriate controller which accepts the facet parameter and logs the event, conversion, or both:

```cs
[HttpPost]
public ActionResult FacetClicked(string facet)
{
    if (String.IsNullOrEmpty(facet))
    {
        return BadRequest();
    }

    _insightsService.LogFacetClicked(facet, "Store facet clicked", AlgoliaSiteSearchModel.IndexName);
    _insightsService.LogFacetConverted(facet, "Store facet converted", AlgoliaSiteSearchModel.IndexName);
    return Ok();
}
```

### Configuring Personalization

Once you've begun to track events using the examples in the previous sections, you can configure a [Personalization strategy](https://www.algolia.com/doc/guides/personalization/personalizing-results/in-depth/configuring-personalization/). This is done directly in the Algolia interface, in your application's __Personalization__ menu.

After your Personalization strategy is configured, you must set certain properties during your search queries to retrieve personalized results:

- __EnablePersonalization__
- __UserToken__: A token which identifies the visitor performing the search. Using the code and examples in this repository, the user token will be the current contact's GUID.
- __X-Forwarded-For__ header: The IP address of the visitor performing the search. See [Algolia's documentation](https://www.algolia.com/doc/guides/getting-analytics/search-analytics/out-of-the-box-analytics/how-to/specify-which-user-is-doing-the-search/#set-the-x-forwarded-for-header).

```cs
var query = new Query(searchText)
{
    Page = page,
    HitsPerPage = PAGE_SIZE,
    ClickAnalytics = true,
    EnablePersonalization = true,
    UserToken = ContactManagementContext.CurrentContact.ContactGUID.ToString()
};
var results = searchIndex.Search<AlgoliaSiteSearchModel>(query, new RequestOptions {
    Headers = new Dictionary<string, string> { { "X-Forwarded-For", Request.HttpContext.Connection.RemoteIpAddress.ToString() } }
});
```

## :chart_with_upwards_trend: Xperience Algolia module

While the Xperience Algolia integration works without an Xperience interface, you may choose to import a custom module into your Xperience website to improve your user's experience. To do so, locate the latest _Kentico.Xperience.AlgoliaSearch_ ZIP package in the root of this repository, download it, and [import it into your Xperience website](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects).

After importing, perform the [necessary steps](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects#Importingasiteorobjects-Importingpackageswithfiles) to include the imported folder `/CMSModules/Kentico.Xperience.AlgoliaSearch` in your project.

![Algolia module grid](/img/index-grid.png)

The newly-imported __Algolia search__ module will provide a listing of all registered Algolia search model code files, along with some statistics directly from Algolia. By default, Algolia indexes are not rebuilt at any point- only updated and newly-created pages are indexed. To rebuild the index completely, use the circular arrow icon at the left of the grid.

To view details about an index, click the eye icon:

![Algolia index content](/img/index-content.png)

Switch to the __Search preview__ tab to perform a basic Algolia query:

![Algolia index preview](/img/index-preview.png)

This view will display the `objectID` and `ClassName` attributes, plus any other searchable attributes which contained the matching search term.
