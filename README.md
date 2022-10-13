![build](https://github.com/Kentico/xperience-algolia/actions/workflows/build.yml/badge.svg)
[![Nuget](https://img.shields.io/nuget/v/Kentico.Xperience.Algolia.KX13)](https://www.nuget.org/packages/Kentico.Xperience.Algolia.KX13)
![Kentico.Xperience.Libraries 13.0.16](https://img.shields.io/badge/Kentico.Xperience.Libraries-v13.0.73-orange)
[![Algolia.Search 6.13.0](https://img.shields.io/badge/Algolia.Search-v6.13.0-blue)](https://www.nuget.org/packages/Algolia.Search#versions-body-tab)

# Xperience Algolia Search Integration

This integration enables the creating of [Algolia](https://www.algolia.com/) search indexes and the indexing of Xperience content tree pages using a code-first approach. Developers can use the [.NET API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/csharp/?client=csharp), [JavaScript API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/javascript/?client=javascript), or [InstantSearch.js](https://www.algolia.com/doc/guides/building-search-ui/what-is-instantsearch/js/) to provide a search interface on their live site.

A single class (created by your developers) contains the Algolia index attributes, the individual attribute configurations, and automatically registers the Algolia index on application startup.

We recommend that you to create a new [.NET Standard 2.0](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) Class Library project to contain the Algolia search models they will create. This project can be referenced by both the CMS and .NET Core projects, allowing developers to reference the stongly-typed search models in each application. As a result, your developers can utilize Algolia's [POCO philosophy](https://www.algolia.com/doc/api-client/getting-started/install/csharp/?client=csharp#poco-types-and-jsonnet) while creating the search interface.

> :warning: When developing the .NET Standard Class Library project, remember to add `[assembly: CMS.AssemblyDiscoverable]` to your code! See [Adding custom assemblies](https://docs.xperience.io/x/ERXfBw).

## :rocket: Installation

1. Install the [Kentico.Xperience.Algolia.KX13](https://www.nuget.org/packages/Kentico.Xperience.Algolia.KX13) NuGet package in both the administration and the live-site project.
2. On the [Algolia dashboard](https://www.algolia.com/dashboard), open your application, navigate to __Settings → API keys__ and note the _Search API key_ value.
3. On the __All API keys__ tab, create a new "Indexing" API key which will be used for indexing and performing searches in the Xperience application. The key must have at least the following ACLs:
  - search
  - addObject
  - deleteObject
  - editSettings
  - listIndexes
4. In the Xperience project's `appsettings.json`, add the following section with your API key values:

```json
"xperience.algolia": {
    "applicationId": "<your application ID>",
    "apiKey": "<your Indexing API key>",
    "searchKey": "<your Search API key>"
}
```
> :warning: Do not use the Admin API key! Use the custom API key you created in step #3.

5. In your administration project's `web.config` files's `appSettings` section, add the following keys:

```xml
<add key="AlgoliaApplicationId" value="<your application ID>"/>
<add key="AlgoliaApiKey" value="<your Indexing API key>"/>
```
6. In your live-site's `Startup.cs`, register the Algolia integration:

```cs
services.AddAlgolia(Configuration);
```

7. (Optional) Import the [Xperience Algolia module](#chart_with_upwards_trend-algolia-search-application-for-administration-interface) in your Xperience website.

## Limitations

It's important to note that Algolia has limitations on the size of your records. If you are indexing content that may contain large amounts of data, we recommend splitting your records into smaller "fragments." Follow the instructions in the [Splitting large content](#scissors-splitting-large-content) section.

## :gear: Creating and registering an Algolia index

An Algolia index and its attributes are defined within a single class file, in which your custom class extends the [`AlgoliaSearchModel`](https://github.com/Kentico/xperience-algolia/blob/master/src/Models/AlgoliaSearchModel.cs) class. Within the class, you define the attributes of the index by creating properties which match the names of Xperience page fields to index. The Xperience fields available may come from the `TreeNode` object, `SKUTreeNode` for products, or any custom page type fields.

```cs
public class SiteSearchModel : AlgoliaSearchModel
{
    public const string IndexName = "SiteIndex";

    [Searchable, Retrievable]
    public string DocumentName { get; set; }

    [MediaUrls, Retrievable, Source(new string[] { nameof(Article.ArticleTeaser), nameof(Coffee.CoffeeImage) })]
    public IEnumerable<string> Thumbnail { get; set; }

    [Searchable, Retrievable, Source(new string[] { nameof(Article.ArticleSummary), nameof(Coffee.CoffeeShortDescription) })]
    public string ShortDescription { get; set; }

    [Searchable, Source(new string[] { nameof(Article.ArticleText), nameof(Coffee.CoffeeDescription) })]
    public string Content { get; set; }

    [Facetable]
    public string CoffeeProcessing { get; set; }

    [Facetable]
    public bool CoffeeIsDecaf { get; set; }
}
```

> :ab: The property names (and names used in the [SourceAttribute](#source-attribute)) are __case-insensitive__. This means that your search model can contain an "articletext" property, or an "ArticleText" property- both will work.

Indexes must be registered during application startup in both the administration application and the live-site application. In the __administration__ project, create a [custom module](https://docs.xperience.io/custom-development/creating-custom-modules/initializing-modules-to-run-custom-code) and use the `OnInit` method to register your indexes with the `IndexStore`:

```cs
protected override void OnInit()
{
    base.OnInit();

    IndexStore.Instance.Add(new AlgoliaIndex(typeof(SiteSearchModel), SiteSearchModel.IndexName));
}
```

The `AlgoliaIndex` constructor also accepts an optional list of site names to which the index is assigned. If not provided, pages from all sites are included. In the __live-site__ project's startup code, modify the `AddAlgolia()` extension method. This method accepts a list of `AlgoliaIndex`, so you can create and register as many indexes as needed

```cs
services.AddAlgolia(Configuration, new AlgoliaIndex[]
{
    new AlgoliaIndex(typeof(SiteSearchModel), SiteSearchModel.IndexName),
    // more indexes...
});
```

If you're developing your search solution in multiple environments (e.g. "DEV" and "STG"), it is recommended that you create a unique Algolia index per environment. With this approach, the search functionality can be tested in each environment individually and changes to the index structure or content will not affect other environments. This can be implemented any way you'd like, including some custom service which transforms the index names. The simplest approach would be to prepend some environment name to the index, which is stored in the application settings:

```cs
var environment = ConfigurationManager.AppSettings["Environment"];
services.AddAlgolia(Configuration, new AlgoliaIndex[]
{
    new AlgoliaIndex(typeof(SiteSearchModel), $"{environment}-{SiteSearchModel.IndexName}")
});
```

### Determining which pages to index

While the above sample code will create an Algolia index, pages in the content tree will not be indexed until one or more [`IncludedPathAttribute`](https://github.com/Kentico/xperience-algolia/blob/master/src/Attributes/IncludedPathAttribute.cs) attributes are applied to the class. The `IncludedPathAttribute` has three properties to configure:

- __AliasPath__: The path of the content tree to index. Use wildcard "/%"  to index all children of a page.
- __PageTypes__ (optional): The code names of the page types under the specified `AliasPath` to index. If not provided, all page types are indexed.
- __Cultures__ (optional): The culture codes of the page language versions to include in the index. If not provided, all culture versions are indexed.

> :bulb: We recommend using the generated [Xperience page type code](https://docs.xperience.io/x/Qw6RBg) to reference page type class names.

When determining which __Cultures__ to index, we recommend creating a new search model per language in your site. This allows Algolia to return more relevant results for that language using stop words and synonyms.

All pages under the specified __AliasPath__ will be indexed regardless of the [permissions](https://docs.xperience.io/x/mgmRBg). This means that if there are publicly-accessible pages and secured pages (e.g. articles only meant for partners), they will all be indexed. If you need to separate this content in your search functionality, you can either:

- Create different sections in your content tree for public and secured pages, and use multiple Algolia indexes to store the information.
- Store the content in the same Algolia index and filter your search results to display the appropriate content to the user. To check whether a user has __Read__ permissions for a page, you can use the  `DocumentSecurityHelper.IsAuthorizedPerDocument()` method.

Below is an example of an Algolia index which includes multiple paths and page types:

```cs
[IncludedPath("/Articles/%", PageTypes = new string[] { Article.CLASS_NAME })]
[IncludedPath("/Store/%", PageTypes = new string[] { "DancingGoatCore.Brewer", "DancingGoatCore.Coffee", "DancingGoatCore.ElectricGrinder", "DancingGoatCore.FilterPack", "DancingGoatCore.ManualGrinder", "DancingGoatCore.Tableware" })]
public class SiteSearchModel : AlgoliaSearchModel
{
    public const string IndexName = "SiteSearchModel";

    [Searchable, Retrievable]
    public string DocumentName { get; set; }

    [Url, Retrievable]
    [Source(new string[] { nameof(SKUTreeNode.SKU.SKUImagePath), nameof(Article.ArticleTeaser) })]
    public string Thumbnail { get; set; }

    [Searchable]
    [Source(new string[] { nameof(SKUTreeNode.DocumentSKUDescription), nameof(Article.ArticleText) })]
    public string Content { get; set; }

    [Searchable, Retrievable]
    [Source(new string[] { nameof(SKUTreeNode.DocumentSKUShortDescription), nameof(Article.ArticleSummary) })]
    public string ShortDescription { get; set; }

    [Retrievable]
    public int SKUID { get; set; }

    [Facetable, Retrievable]
    public decimal? SKUPrice { get; set; }

    [Retrievable]
    public int SKUPublicStatusID { get; set; }

    [Retrievable]
    public DateTime DocumentCreatedWhen { get; set; }

    [Facetable]
    public string CoffeeProcessing { get; set; }

    [Facetable]
    public bool CoffeeIsDecaf { get; set; }
}
```

### Customizing the indexing process

In some cases, you may want to customize the values that are sent to Algolia during page indexing. For example, in the search model above there is a `Content` property which retrieves its value from the `DocumentSKUDescription` or `ArticleText` columns. However, if we are indexing the "About Us" page in Dancing Goat, the content of the page actually comes from the child pages.

To customize the indexing process, you can override the `OnIndexingProperty()` that is defined in the search model base class `AlgoliaSearchModel`. This method is called during the indexing of a page for each property defined in your search model. You can use the function parameters such as the page being indexed, the value that would be indexed, the search model property name, and the name of the database column the value was retrieved from.

To index the data from the child pages and store it in the "About Us" record in Algolia, we can use this method to loop through the child pages and retrieve text from their fields:

```cs
public override object OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
{
    switch (propertyName)
    {
        case nameof(Content):
            if (node.DocumentName == "About Us")
            {
                var text = new StringBuilder();
                var aboutUsSections = node.Children.WithAllData.Where(child => child.ClassName == AboutUsSection.CLASS_NAME);
                foreach (var aboutUsSection in aboutUsSections)
                {
                    text.Append(aboutUsSection.AboutUsSectionText);
                }
                return text.ToString();
            }
            break;
    }

    return foundValue;
}
```

### Creating a page crawler

The default behavior of the integration is to index structured content from your Xperience website. However, you can also "crawl" your pages to retrieve the text that your visitors see on the front-end, and index that text in an Algolia attribute. Use the `OnIndexingProperty` method mentioned above to crawl your pages. For example, if your search model has a property named "Content," you can set the value like this:

```cs
public override object OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
{
    if (propertyName == nameof(Content))
    {
        return GetCrawlerContent(node);
    }

    return foundValue;
}

private string GetCrawlerContent(TreeNode node)
{
    var crawler = new SearchCrawler();
    crawler.CrawlerUser = UserInfoProvider.AdministratorUserName; // Or, set your own user
    var contentProcessor = Service.Resolve<ISearchCrawlerContentProcessor>();
    var url = DocumentURLProvider.GetAbsoluteUrl(node);
    if (String.IsNullOrEmpty(url))
    {
        return String.Empty;
    }

    try
    {
        var html = crawler.DownloadHtmlContent(url);
        if (!String.IsNullOrEmpty(html))
        {
            var plainText = contentProcessor.Process(html);
            var bytes = plainText.Length * sizeof(Char);
            // Consider trimming if text is too large..

            return plainText;
        }
    }
    catch (Exception ex)
    {
        // Handle errors..
    }

    return String.Empty;
}
```

It's important to note that Algolia has [limitations](https://support.algolia.com/hc/en-us/articles/4406981897617-Is-there-a-size-limit-for-my-index-records-/) on the size of your records, so you may want to check the size of the crawled text and trim it if necessary. Also, this integration will only re-index an updated page if one of the indexed columns is modified. In the case that your search model has a "Content" property, but the page type fields like "ArticleText" aren't included in the search model, you should use the [`SourceAttribute`](#source-attribute) to indicate which page type fields are considered part of the page content:

```cs
// The page will be crawled when either "ArticlePostDate" or "ArticleText" are updated (or, on a full rebuild and new page creation)
[Searchable]
[Source(new string[] { nameof(Article.ArticlePostDate), nameof(Article.ArticleText) })]
public string Content { get; set; }
```

## :memo: Configuring Algolia attributes

This package includes five attributes which can be applied to each individual Algolia attribute to further configure the Algolia index:

- [`Searchable`](#searchable-attribute)
- [`Facetable`](#facetable-attribute)
- [`Retrievable`](#retrievable-attribute)
- [`Source`](#source-attribute)
- [`Url`](#url-attribute)

### __Searchable__ attribute

This attribute indicates that an Algolia attribute is [searchable](https://www.algolia.com/doc/api-reference/api-parameters/searchableAttributes/#how-to-use). You can define optional attribute properties to adjust the performance of your searchable attributes:

- __Order__ (optional): Attributes with lower `Order` will be given priority when searching for text. Attributes without `Order` set will be added to the end of the list (making them lower priority), while attributes with the same `Order` will be added with the same priority and are automatically `Unordered`.
- __Unordered__ (optional): By default, matches at the beginning of a text are more relevant than matches at the end of the text. If set to `true`, the position of the matched text in the attribute content is irrelevant.

```cs
[Searchable]
public string DocumentName { get; set; }

[Searchable(Order = 0)] // Highest priority
public string DocumentName { get; set; }

[Searchable(Unordered = true)]
public string DocumentName { get; set; }
```

### __Facetable__ attribute

This attribute indicates that an Algolia attribute is a [facet or filter](https://www.algolia.com/doc/api-reference/api-parameters/attributesForFaceting/#how-to-use). By creating facets, your developers are able to create a [faceted search](https://www.algolia.com/doc/guides/managing-results/refine-results/faceting/) interface on the front-end application. Optional attribute properties can be defined to change the functionality of your faceted attributes:

- __FilterOnly__ (optional): Defines the attribute as a filter and not a facet. If you do not need facets, defining an attribute as a filter reduces the size of the index and improves the speed of the search.

- __Searchable__ (optional): Allows developers to search for values within a facet, e.g. via the [`SearchForFacetValues()`](https://www.algolia.com/doc/api-reference/api-methods/search-for-facet-values/) method.

- __UseAndCondition__ (optional): When using the sample code in this repository and the `AlgoliaFacetFilterViewModel` class, facet conditions of the same properties are joined by "OR" by default. For example, `(CoffeProcessing:washed OR CoffeeProcessing:natural)`. You may set this property to __true__ to join them by "AND" instead.

> :warning: A property cannot be both `FilterOnly` and `Searchable`, otherwise an exception will be thrown.

```cs
[Facetable]
public decimal? SKUPrice { get; set; }

[Facetable(FilterOnly = true)] // Filter
public decimal? SKUPrice { get; set; }

[Facetable(Searchable = true)] // Searchable
public decimal? SKUPrice { get; set; }
```

### __Retrievable__ attribute

This attribute determines which Algolia attributes to [retrieve when searching](https://www.algolia.com/doc/api-reference/api-parameters/attributesToRetrieve/#how-to-use). Reducing the amount of attributes retrieved will help improve the speed of your searches, without impacting the search functionality.

```cs
[Searchable, Retrievable] // Used during searching and retrieved
public string DocumentName { get; set; }

[Searchable] // Used in searching but not retrieved
public string ArticleText { get; set; }
```

### __Source__ attribute

This attribute can be used to alter the page field that the attribute value is retrieved from. This can be useful in indexes which include multiple page types, but the different page type fields should be stored in the same Algolia attribute. For example, your index should contain a "Thumbnail" attribute containing the URL to an image, but the image for each page type is stored in different page fields.

Columns specified in the `Source` attribute are parsed in the order they appear, until a non-empty string and non-null value is found, which is then indexed. We recommend referencing standard page fields and custom page type fields using `nameof()` to avoid typos.

```cs
[Url, Retrievable]
[Source(new string[] { nameof(SKUTreeNode.SKU.SKUImagePath), nameof(Article.ArticleTeaser) })]
public string Thumbnail { get; set; }
```

### __Url__ attribute

This attribute indicates that the value of the page field should be converted into an absolute live-site URL before indexing. This can be useful when configuring the [Display Preferences](https://www.algolia.com/doc/guides/managing-results/rules/merchandising-and-promoting/how-to/how-to-configure-and-use-the-visual-editor-with-category-pages/#configure-the-visual-editor) in Algolia, for example. This attribute can be used on a page type field which stores a URL as a relative URL (`~/getmedia`) or one that stores an Xperience attachment.

```cs
[Url, Retrievable] // Attachment field
public string ArticleTeaser { get; set; }

[Url, Retrievable] // Multiple fields
[Source(new string[] { nameof(SKUTreeNode.SKU.SKUImagePath), nameof(Article.ArticleTeaser) })]
public string Thumbnail { get; set; }
```

## :scissors: Splitting large content

Due to [limitations](https://support.algolia.com/hc/en-us/articles/4406981897617-Is-there-a-size-limit-for-my-index-records-/) on the size of Algolia records, we recommend splitting large content into smaller fragments. This operation is performed automatically during indexing by [`IAlgoliaObjectGenerator.SplitData()`](/src/Services/IAlgoliaObjectGenerator.cs), but there is no data splitting by default.

To enable data splitting for an Algolia index, add the `DistinctOptions` parameter during registration:

```cs
IndexStore.Instance.Add(new AlgoliaIndex(typeof(SiteSearchModel), SiteSearchModel.IndexName, distinctOptions: new DistinctOptions(nameof(SiteSearchModel.DocumentName), 1)))
```

The `DistinctOptions` constructor accepts two parameters:

  - __distinctAttribute__: Corresponds with [this Algolia setting](https://www.algolia.com/doc/api-reference/api-parameters/attributeForDistinct). This is a property of the search model whose value will remain constant for all fragments, and is used to identify fragments during de-duplication. Fragments of a search result are "grouped" together according to this attribute's value, then a certain number of fragments per-group are returned, depending on the `distinctLevel` setting. In most cases, this will be a property like `DocumentName` or `NodeAliasPath`.
  - __distinctLevel__: Corresponds with [this Algolia setting](https://www.algolia.com/doc/api-reference/api-parameters/distinct). A value of zero disables de-duplication and grouping, while positive values determine how many fragments will be returned by a search. This is generally set to "1" so that only one fragment is returned from each grouping.

To implement data splitting, create and register a custom implementation of `IAlgoliaObjectGenerator`. It's __very important__ to set the "objectID" of each fragment, as seen in the below example. The IDs can be any arbitrary string, but setting this ensures that the fragments are updated and deleted properly when the page is modified. We recommend developing a consistent naming strategy like in the example below, where an index number is appended to the original ID. The IDs should _not_ be random! Calling `SplitData()` on the same node multiple times should always generate the same fragments and IDs.

In the following example, we have large articles on our website which can be split into smaller fragments by splitting text on the `<p>` tag. Note that each fragment still contains all of the original data- only the "Content" property is modified.

```cs
[assembly: RegisterImplementation(typeof(IAlgoliaObjectGenerator), typeof(CustomAlgoliaObjectGenerator))]
namespace DancingGoat.Algolia
{
  public class CustomAlgoliaObjectGenerator : IAlgoliaObjectGenerator
  {
      private readonly IAlgoliaObjectGenerator defaultImplementation;

      public CustomAlgoliaObjectGenerator(IAlgoliaObjectGenerator defaultImplementation)
      {
          this.defaultImplementation = defaultImplementation;
      }

      public JObject GetTreeNodeData(AlgoliaQueueItem queueItem)
      {
          return defaultImplementation.GetTreeNodeData(queueItem);
      }

      public IEnumerable<JObject> SplitData(JObject originalData, AlgoliaIndex algoliaIndex)
      {
          if (algoliaIndex.Type == typeof(SiteSearchModel))
          {
              return SplitParagraphs(originalData, nameof(SiteSearchModel.Content));
          }

          return new JObject[] { originalData };
      }

      private IEnumerable<JObject> SplitParagraphs(JObject originalData, string propertyToSplit)
      {
          var originalId = originalData.Value<string>("objectID");
          var content = originalData.Value<string>(propertyToSplit);
          if (string.IsNullOrEmpty(content))
          {
              return new JObject[] { originalData };
          }

          List<string> paragraphs = new List<string>();
          var matches = Regex.Match(content, @"<p>\s*(.+?)\s*</p>");
          while (matches.Success)
          {
              paragraphs.Add(matches.Value);
              matches = matches.NextMatch();
          }

          return paragraphs.Select((p, index) => {
              var data = (JObject)originalData.DeepClone();
              data["objectID"] = $"{originalId}-{index}";
              data[propertyToSplit] = p;
              return data;
          });
      }
  }
}
```

## :mag_right: Implementing the search interface

You can use Algolia's [.NET API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/csharp/?client=csharp), [JavaScript API](https://www.algolia.com/doc/api-client/getting-started/what-is-the-api-client/javascript/?client=javascript), or [InstantSearch.js](https://www.algolia.com/doc/guides/building-search-ui/what-is-instantsearch/js/) to implement a search interface on your live site. The following example will help you with creating a search interface for .NET Core. In your Controllers, you can get a `SearchIndex` object by injecting the `IAlgoliaIndexService` interface and calling the `InitializeIndex()` method on the client using your index's code name. Then, construct a `Query` to search the Algolia index. Algolia's pagination is zero-based, so in the Dancing Goat sample project we subtract 1 from the current page number:

```cs
private readonly IAlgoliaIndexService _indexService;

public SearchController(IAlgoliaIndexService indexService)
{
    _indexService = indexService;
}

public ActionResult Search(string searchText, int page = DEFAULT_PAGE_NUMBER)
{
    page = Math.Max(page, DEFAULT_PAGE_NUMBER);

    var searchIndex = _indexService.InitializeIndex(SiteSearchModel.IndexName);
    var query = new Query(searchText)
    {
        Page = page - 1,
        HitsPerPage = PAGE_SIZE
    };

    try
    {
        var results = searchIndex.Search<SiteSearchModel>(query);
        //...
    }
    catch (Exception e)
    {
        //...
    }
}
```

The `Hits` object of the [search response](https://www.algolia.com/doc/api-reference/api-methods/search/?client=csharp#response) will be a list of the strongly typed objects defined by your search model (`SiteSearchModel` in the above example). Other helpful properties of the results are `NbPages` and `NbHits`.

The properties of each hit will be populated from the Algolia index, but be sure to check for `null` values! For example, a property that does _not_ have the [`Retrievable`](#retrievable-attribute) attribute will not be returned and custom page type fields will only be present for results of that type. That is, a property named "ArticleText" will most likely be `null` for products on your site. You can reference the [`AlgoliaSearchModel.ClassName`](https://github.com/Kentico/xperience-algolia/blob/master/src/Models/AlgoliaSearchModel.cs#L27) property present on all indexes to check the type of the returned hit.

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

In the view, loop through the `Hits` and display the results using a [display template](https://docs.microsoft.com/en-us/dotnet/api/system.web.mvc.html.displayextensions.displayfor?view=aspnet-mvc-5.2). You can define separate display templates for products or each page type:

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

In the display template, reference your the properties of your search model to display the result:

```cshtml
@model DancingGoat.SiteSearchModel

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

### Working with scheduled content

Pages which use the __Publish From__ and __Publish To__ fields remain in the Algolia index after they are [unpublished](https://docs.xperience.io/x/8A6RBg#UsingAzureCognitiveSearch-Indexingpageswithsetpublishingintervals). If you want to filter them out of your search results, you must add a condition in your search code.

This repository automatically indexes the `DocumentPublishFrom` and `DocumentPublishTo` columns and converts them to a Unix timestamp in UTC as [recommended by Algolia](https://www.algolia.com/doc/guides/managing-results/refine-results/filtering/how-to/filter-by-date/). To remove pages that have not reached their publish date or have been unpublished, get the current Unix timestamp in UTC and use the following condition:

```cs
var nowUnixTimestamp = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
var query = new Query(searchText)
{
    Filters = $"{nameof(SiteSearchModel.DocumentPublishFrom)} <= {nowUnixTimestamp} AND {nameof(SiteSearchModel.DocumentPublishTo)} > {nowUnixTimestamp}"
};
```

### Creating an autocomplete search box

Algolia provides [autocomplete](https://www.algolia.com/doc/ui-libraries/autocomplete/introduction/what-is-autocomplete/) functionality via javascript which you can [install](https://www.algolia.com/doc/ui-libraries/autocomplete/introduction/getting-started/#installation) and set up any way you'd like. Below is an example of how to add autocomplete functionality to the Dancing Goat demo site's main search box.

1. In the `/_Layout.cshtml` view which is rendered for every page, add a reference to Algolia's scripts and the default theme for autocomplete:

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
@using Microsoft.Extensions.Options
@inject IOptions<AlgoliaOptions> options

@{
    var algoliaOptions = options.Value;
}
```

4. Add a script near the end of the `<body>` which loads your Algolia index. Be sure to use your __Search API Key__ which is public, and _not_ your __Admin API Key__!

```js
<script type="text/javascript">
    var client = algoliasearch('@algoliaOptions.ApplicationId', '@algoliaOptions.SearchKey');
    var index = client.initIndex('@SiteSearchModel.IndexName');
</script>
```

5. Initialize the autocomplete search box, then create a handler for when users click on autocomplete suggestions, and when the _Enter_ button is used:

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

When you build and run the Dancing Goat website and start typing into the search box, records from the Algolia index will be suggested:

![Autocomplete default theme](/img/autocomplete-default-theme.png)

#### Customizing the autocomplete search box

In our sample implementation of the Algolia autocomplete search box,the standard [Autocomplete classic theme](https://www.algolia.com/doc/ui-libraries/autocomplete/introduction/getting-started/#install-the-autocomplete-classic-theme) was used for basic styling of the search box and the autocomplete suggestion layout. You can reference the theme's [CSS classes and variables](https://www.algolia.com/doc/ui-libraries/autocomplete/api-reference/autocomplete-theme-classic/) to customize the appearance of the search box to match the design of your website.

In the Dancing Goat website, you can add the following to the CSS which styles the search box and suggestions to match the Dancing Goat theme:

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

The layout of each individual suggestion can be customized by providing a [custom template](https://www.algolia.com/doc/ui-libraries/autocomplete/core-concepts/templates/) in the `autocomplete()` function. In the Dancing Goat website, you can add an image to each suggestion and highlight the matching search term by adding the following to your script:

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

> :warning: The attributes `DocumentName` and `Thumbnail` used in this example are not present in all Algolia indexes! If you follow this example, make sure you are using attributes present in your index. See the [sample search model](#determining-which-pages-to-index) to find out how these attributes were defined.

This is the final result of adding our custom CSS and template:

![Autocomplete custom template](/img/autocomplete-custom-template.png)

## :ballot_box_with_check: Faceted search

As the search interface can be designed in multiple languages using Algolia's APIs, your developers can implement [faceted search](https://www.algolia.com/doc/guides/managing-results/refine-results/faceting/). However, this repository contains some helpful classes to develop faceted search using C#. The following is an example of creating a faceted search interface within the Dancing Goat sample site's store.

### Setting up basic search

The Dancing Goat store doesn't use search out-of-the-box, so first you need to hook it up to Algolia. In this example, the search model seen in [Determining which pages to index](#determining-which-pages-to-index) will be used.

1. Inject `IAlgoliaIndexService` into the `CoffeesController` as shown in [this section](#mag_right-implementing-the-search-interface).

2. In __CoffeesController.cs__, create a method that will perform a standard Algolia search. In the `Query.Filters` property, add a filter to only retrieve records where `ClassName` is `DancingGoatCore.Coffee.` You also specify which `Facets` you want to retrieve, but they are not used yet.

```cs
private SearchResponse<SiteSearchModel> Search()
{
    var facetsToRetrieve = new string[] {
        nameof(SiteSearchModel.CoffeeIsDecaf),
        nameof(SiteSearchModel.CoffeeProcessing)
    };

    var defaultFilter = $"{nameof(SiteSearchModel.ClassName)}:{new Coffee().ClassName}";
    var query = new Query()
    {
        Filters = defaultFilter,
        Facets = facetsToRetrieve
    };

    var searchIndex = _indexService.InitializeIndex(SiteSearchModel.IndexName);
    return searchIndex.Search<SiteSearchModel>(query);
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
        public SiteSearchModel Hit { get; set; }

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

        public AlgoliaStoreModel(SiteSearchModel hit)
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

4. In __ProductListViewModel.cs__, change the `Items` property to be a list of the new `AlgoliaStoreModel` items:

```cs
public IEnumerable<AlgoliaStoreModel> Items { get; set; }
```

5. Modify the `Index()` method to perform the search and provide the list of hits converted into `AlgoliaStoreModel` objects:

```cs
[HttpGet]
[HttpPost]
public ActionResult Index()
{
    try
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
    catch (Exception ex)
    {
        // Log error..
        return View(new ProductListViewModel
        {
            Items = Enumerable.Empty<AlgoliaStoreModel>()
        });
    }
}
```

6. Modify the _Index.cshtml_, _CoffeeList.cshtml_, and _ProductListItem.cshtml_ views to display your Algolia products.

### Filtering your search with facets

In the `Search()` method, the _CoffeeIsDecaf_ and _CoffeeProcessing_ facets are retrieved from Algolia, but they are not used yet. In the following steps you will use an `AlgoliaFacetFilter` (which implements `IAlgoliaFacetFilter`) to hold the facets and the current state of the faceted search interface. The `UpdateFacets()` method of this interface allows you convert the facet response into a list of `AlgoliaFacetedAttribute`s which contains the attribute name (e.g. "CoffeeIsDecaf"), localized display name (e.g. "Decaf"), and a list of `AlgoliaFacet` objects.

Each `AlgoliaFacet` object represents the faceted attribute's possible values and contains the number of results that will be returned if the facet is enabled. For example, the "CoffeeProcessing" `AlgoliaFacetedAttribute` contains 3 `AlgoliaFacet` objects in its `Facets` property.

1. In the `Search()` method, add a parameter that accepts an `IAlgoliaFacetFilter`. Then, check whether the `filter` is non-null and call the `GetFilter()` method to generate the facet filters:

```cs
private SearchResponse<SiteSearchModel> Search(IAlgoliaFacetFilter filter = null)
{
    var facetsToRetrieve = new string[] {
        nameof(SiteSearchModel.CoffeeIsDecaf),
        nameof(SiteSearchModel.CoffeeProcessing)
    };

    var defaultFilter = $"{nameof(SiteSearchModel.ClassName)}:{new Coffee().ClassName}";
    if (filter != null)
    {
        var facetFilter = filter.GetFilter(typeof(SiteSearchModel));
        if (!String.IsNullOrEmpty(facetFilter))
        {
            defaultFilter += $" AND {facetFilter}";
        }
    }

    var query = new Query()
    {
        Filters = defaultFilter,
        Facets = facetsToRetrieve
    };


    var searchIndex = _indexService.InitializeIndex(SiteSearchModel.IndexName);
    return searchIndex.Search<SiteSearchModel>(query);
}
```

The `GetFilter()` method returns a condition for each facet in the `IAlgoliaFacetFilter` which has the `IsChecked` property set to true. Facets with the same attribute name are grouped within an "OR" condition. For example, if a visitor on your store listing checked the boxes for decaf coffee with the "washed" and "natural" processing type, the filter will look like this:

> "CoffeeIsDecaf:true" AND ("CoffeeProcessing:washed" OR "CoffeeProcessing:natural")

You can change this behavior by setting the [`UseAndCondition`](#facetable-attribute) property of your faceted attributes, or by registering your own implementation of `IAlgoliaFacetFilter`.

2. In `ProductListViewModel.cs`, add another property which contains the facet filter:

```cs
public IAlgoliaFacetFilter AlgoliaFacetFilter { get; set; }
```

3. Modify the `Index()` action to accept an `AlgoliaFacetFilter` parameter, pass it to the `Search()` method, parse the facets from the search response, then pass the filter to the view:

```cs
[HttpGet]
[HttpPost]
public ActionResult Index(AlgoliaFacetFilterViewModel filter)
{
    ModelState.Clear();
    try
    {
        var searchResponse = Search(filter);
        var items = searchResponse.Hits.Select(
            hit => new AlgoliaStoreModel(hit)
        );

        filter.UpdateFacets(new FacetConfiguration(searchResponse.Facets));

        var model = new ProductListViewModel
        {
            Items = items,
            AlgoliaFacetFilter = filter
        };

        return View(model);
    }
    catch (Exception ex)
    {
        // Log error..
        return View(new ProductListViewModel
        {
            Items = Enumerable.Empty<AlgoliaStoreModel>(),
            AlgoliaFacetFilter = filter
        });
    }
}
```

Here, the `UpdateFacets()` method accepts the facets returned from Algolia. Because the entire list of available facets depends on the Algolia response, and the facets in your filter are replaced with new ones, this method ensures that a facet that was used previously (e.g. "CoffeeIsDecaf:true") maintains it's enabled state when reloading the search interface.

### Displaying the facets

The Dancing Goat store listing now uses Algolia search, and you have a filter which contains Algolia facets and properly filters the search results. The final step is to display the facets in the store listing and handle user interaction with the facets.

1. In _Index.cshtml_, replace the existing filter with custom view and set the form action to "Index" as we will be reloading the entire layout:

```html
<aside class="col-md-4 col-lg-3 product-filter">
    <form asp-controller="Coffees" asp-action="Index">
        <partial name="~/Views/Shared/Algolia/_AlgoliaFacetFilter.cshtml" model="Model.AlgoliaFacetFilter" />
    </form>
</aside>
```

2. In the `Scripts` section of the view, remove the existing script and add a script that will post the form when a checkbox is toggled:

```html
<script>
    $(function () {
        $('.js-postback input:checkbox').change(function () {
            $(this).parents('form').submit();
        });
    });
</script>
```

3. Create a _/Views/Shared/Algolia/\_AlgoliaFacetFilter.cshtml_ view. As you can see in step 1, this view will accept our facet filter and loops through each `AlogliaFacetedAttribute` it contains:

```cshtml
@using Kentico.Xperience.Algolia.Models
@model AlgoliaFacetFilter

@for (var i=0; i<Model.FacetedAttributes.Count(); i++)
{
    @Html.HiddenFor(m => Model.FacetedAttributes[i].Attribute)
    @Html.EditorFor(model => Model.FacetedAttributes[i], "~/Views/Shared/Algolia/EditorTemplates/_AlgoliaFacetedAttribute.cshtml")
}
```

4. For each `AlgoliaFacetedAttribute` you now want to loop through each `AlgoliaFacet` it contains and display a checkbox that will enable the facet for filtering. Create a _/Views/Shared/Algolia/EditorTemplates/\_AlgoliaFacetedAttribute.cshtml_ file and render inputs for each facet:

```cshtml
@using Kentico.Xperience.Algolia.Models
@model AlgoliaFacetedAttribute

<h4>@Model.DisplayName</h4>
@for (var i = 0; i < Model.Facets.Count(); i++)
{
    @Html.HiddenFor(m => Model.Facets[i].Value)
    @Html.HiddenFor(m => Model.Facets[i].Attribute)
    <span class="checkbox js-postback">
        <input data-facet="@(Model.Attribute):@Model.Facets[i].Value" asp-for="@Model.Facets[i].IsChecked" />
        <label asp-for="@Model.Facets[i].IsChecked">@Model.Facets[i].DisplayValue (@Model.Facets[i].Count)</label>
    </span>
}
```

Now, when you check one of the facets your JavaScript code will cause the form to post back to the `Index()` action. The `filter` parameter will contain the facets that were displayed on the page, with the `IsChecked` property of each facet set accordingly. The filter is passed to our `Search()` method which uses `GetFilter()` to filter the search results, and a new `AlgoliaFacetFilterViewModel` is created with the results of the query.

![Dancing goat facet example](/img/dg-facets.png)

### Translating facet names and values

Without translation, the view will display facet attribute names (e.g. "CoffeeIsDecaf") instead of a human-readable title like "Decaffeinated," and values like "true" and "false." The `FacetConfiguration` model accepted by `IAlgoliaFacetFilter.UpdateFacets()` contains the `displayNames` parameter which can be used to translate facets into any text you'd like.

1. Create a new class (or use an existing class) to hold a `Dictionary<string, string>` containing the translations:

```cs
public class AlgoliaFacetTranslations
```

2. Add entries to the dictionary with keys in the format _[AttributeName]_ or _[AttributeName].[Value]_ for faceted attributes or facet values, respectively:

```cs
  public static Dictionary<string, string> CoffeeTranslations
  {
      get
      {
          return new Dictionary<string, string>
          {
              { nameof(SiteSearchModel.CoffeeIsDecaf), "Decaffeinated" },
              { $"{nameof(SiteSearchModel.CoffeeIsDecaf)}.true", "Yes" },
              { $"{nameof(SiteSearchModel.CoffeeIsDecaf)}.false", "No" },
              { nameof(SiteSearchModel.CoffeeProcessing), "Processing" },
              { $"{nameof(SiteSearchModel.CoffeeProcessing)}.washed", "Washed" },
              { $"{nameof(SiteSearchModel.CoffeeProcessing)}.natural", "Natural" }
          };
      }
  }
```

3. Reference this dictionary when calling the `UpdateFacets()` method in your search interface:

```cs
var searchResponse = await Search(filter, cancellationToken);
filter.UpdateFacets(new FacetConfiguration(searchResponse.Facets, AlgoliaFacetTranslations.CoffeeTranslations));
```

## :bulb: Personalizing search results

Algolia offers search result [personalization](https://www.algolia.com/doc/guides/personalization/what-is-personalization/) to offer more relevant results to each individual visitor on your website. To begin personalizing search results, you first need to send [events](https://www.algolia.com/doc/guides/sending-events/planning/) to Algolia which detail the visitor's activity. As with much of the Algolia functionality, sending events is very flexible depending on your API of choice and how your search is implemented. You can choose to use any of the approaches in the Algolia documentation (e.g. [Google Tag Manager](https://www.algolia.com/doc/guides/sending-events/implementing/connectors/google-tag-manager/)). The following section showcases how to send events using C# with the assistance of some classes from this repository.


If you do not already have a basic search interface set up, you need to [implement a search interface](#mag_right-implementing-the-search-interface).

### Sending search result click events/conversions

To track these types of events, the `ClickAnalytics` property must be enabled when creating your search query:

```cs
var query = new Query(searchText)
{
    Page = page,
    HitsPerPage = PAGE_SIZE,
    ClickAnalytics = true
};
```

This repository uses query string parameters to track the required data for submitting search result clicks and conversion to Algolia. As all search models extend `AlgoliaSearchModel` and contain a `Url` property, you can call `IAlgoliaInsightsService.SetInsightsUrls()` to update the URL of your results with all the necessary data:

```cs
// Inject IAlgoliaInsightsService
public SearchController(IAlgoliaInsightsService algoliaInsightsService)
{
    _algoliaInsightsService = algoliaInsightsService;
}

// In your search method, call SetInsightsUrls
var results = searchIndex.Search<SiteSearchModel>(query);
_algoliaInsightsService.SetInsightsUrls(results);
```

Now, when you display the search results using the `Url` property, it will look something like _https://mysite.com/store/brewers/aeropress/?object=88&pos=2&query=d057994ba21f0a56c75511c2c005f49f_. To submit the event to Algolia when your visitor clicks this link, inject `IAlgoliaInsightsService` into the view that renders the linked page. Or, you can inject it into the view which renders all pages, e.g. _\_Layout.cshtml_. Call `LogSearchResultClicked()`, `LogSearchResultConversion()`, or both methods of the service:

```cshtml
@inject IAlgoliaInsightsService _insightsService

@{
    await _insightsService.LogSearchResultClicked("Search result clicked", SiteSearchModel.IndexName, CancellationToken.None);
    await _insightsService.LogSearchResultConversion("Search result converted", SiteSearchModel.IndexName, CancellationToken.None);
}
```

When a visitor lands on a page after clicking a search result, these methods use the data contained in the query string to submit a search result click event or conversion. If the visitor arrives on the page without query string parameters (e.g. using the site navigation), nothing is logged.

### Sending generic page-related events/conversions

Aside from search result related events/conversions, there are many more generic events you can send to Algolia. For example, a very important conversion on E-commerce websites could be _Product added to cart_. For sites that produce blog posts or articles, you may want to send an _Article viewed_ event.

For a conversion, you can use the `IAlgoliaInsightsService.LogPageConversion()` method in your controllers or views. In the Dancing Goat sample site, we can log a _Product added to cart_ conversion in the __CheckoutController__:

```cs
public async Task<ActionResult> AddItem(CartItemUpdateModel item)
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
            await _insightsService.LogPageConversion(page.DocumentID, "Product added to cart", SiteSearchModel.IndexName, CancellationToken.None);
        }
        
    }

    return RedirectToAction("ShoppingCart");
}
```

You can also log an event when a visitor simply views a page with the `LogPageViewed()` method. For example, in the __ArticlesController__ you can log an _Article viewed_ event:

```cs
public async Task<IActionResult> Detail([FromServices] ArticleRepository articleRepository)
{
    var article = articleRepository.GetCurrent();

    await _insightsService.LogPageViewed(article.DocumentID, "Article viewed", SiteSearchModel.IndexName, CancellationToken.None);

    return new TemplateResult(article);
}
```

Or, in the _\_Details.cshtml_ view for products, you can log a _Product viewed_ event:

```cshtml
@inject IAlgoliaInsightsService _insightsService
@inject IPageDataContextRetriever _pageDataContextRetriever

@{
    if(_pageDataContextRetriever.TryRetrieve<TreeNode>(out var context))
    {
        await _insightsService.LogPageViewed(context.Page.DocumentID, "Product viewed", SiteSearchModel.IndexName, CancellationToken.None);
    }
}
```

### Logging facet-related events/conversions

You can log events and conversions when facets are displayed to a visitor, or when they click on an individual facet. In this example, the code from our Dancing Goat faceted search [example](#filtering-your-search-with-facets) will be used. Logging a _Search facets viewed_ event can be done in the `Index()` action of __CoffeesController__. The `LogFacetsViewed()` method requires a list of `AlgoliaFacetedAttribute`s, which you can get from the filter:

```cs
var searchResponse = Search(filter);
filter.UpdateFacets(new FacetConfiguration(searchResponse.Facets));
await _insightsService.LogFacetsViewed(filter.FacetedAttributes, "Store facets viewed", SiteSearchModel.IndexName, CancellationToken.None);
```

To log an event or conversion when a facet is clicked, you need to use AJAX. First, in the _\_AlgoliaFacetedAttribute.cshtml_ view which displays each check box, add a `data` attribute that stores the facet name and value (e.g. "CoffeeIsDecaf:true"):

```cshtml
<input data-facet="@(Model.Attribute):@Model.Facets[i].Value" asp-for="@Model.Facets[i].IsChecked" />
```

In the _Index.cshtml_ view for the coffee listing, the `change()` function is already used to run some javascript when a facet is checked or unchecked. Let's add some code that runs only if the facet has been checked which gets the value of the new `data` attribute and sends a POST request:

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

This sends the request to the __StoreController__ `FacetClicked()` action, but you can send the request anywhere else. Check the __Startup.cs__ to make sure your application can handle this request:

```cs
endpoints.MapControllerRoute(
    name: "facetClicked",
    pattern: "Algolia/FacetClicked/{facet?}",
    defaults: new { controller = "Store", action = "FacetClicked" }
);
```

In the appropriate controller, create the action which accepts the facet parameter and logs the event, conversion, or both:

```cs
[HttpPost]
public async Task<ActionResult> FacetClicked(string facet, CancellationToken ct)
{
    if (String.IsNullOrEmpty(facet))
    {
        return BadRequest();
    }

    await _insightsService.LogFacetClicked(facet, "Store facet clicked", SiteSearchModel.IndexName, ct);
    await _insightsService.LogFacetConverted(facet, "Store facet converted", SiteSearchModel.IndexName, ct);
    return Ok();
}
```

### Configuring Personalization

Once you've begun to track events using the examples in the previous sections, you can configure a [personalization strategy](https://www.algolia.com/doc/guides/personalization/personalizing-results/in-depth/configuring-personalization/). This is done directly in the Algolia interface, in your application's __Personalization__ menu.

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
var results = searchIndex.Search<SiteSearchModel>(query, new RequestOptions {
    Headers = new Dictionary<string, string> { { "X-Forwarded-For", Request.HttpContext.Connection.RemoteIpAddress.ToString() } }
});
```

## :crystal_ball: Using InstantSearch.js

InstantSearch.js is a vanilla javascript library developed by Algolia which utilizes highly-customizable widgets to easily develop a search interface with nearly no coding. In this example, we will use InstantSearch.js in the Dancing Goat sample site with very few changes, using the search model sample code [here](#determining-the-pages-to-index).

1. Create a new empty Controller to display the search (e.g. __InstantsearchController__), and ensure it has a proper route in `Startup.cs`:

```cs
endpoints.MapControllerRoute(
    name: "instantsearch",
    pattern: "Algolia/Instantsearch",
    defaults: new { controller = "Instantsearch", action = "Index" }
);
```

2. Create the _Index.cshtml_ view for your controller with the basic layout and stylesheet references. Load your Algolia settings for use later:

```cshtml
@using Microsoft.Extensions.Options
@inject IOptions<AlgoliaOptions> options

@{
    var algoliaOptions = options.Value;
}

@section styles {
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/instantsearch.css@7/themes/algolia-min.css" />
    <link rel="stylesheet" href="~/Content/Styles/instantsearch.css" />
}

<div class="row instantsearch-container">
    <div class="left-panel">
        <div id="clear-refinements"></div>

        <h2>Decaf</h2>
        <div id="decaf-list"></div>

        <h2>Processing</h2>
        <div id="processing-list"></div>

        <h2>Type</h2>
        <div id="type-list"></div>
    </div>
    <div class="right-panel">
        <div id="searchbox" class="ais-SearchBox"></div>
        <div id="hits"></div>
        <div id="pagination"></div>
    </div>
</div>
```

3. At the bottom of your view, add a `scripts` section which loads the InstantSearch.js scripts, initializes the search widget, three faceting widgets, the results widget, and pagination widget:

```cshtml
@section scripts {
    <script src="https://cdn.jsdelivr.net/npm/algoliasearch@4/dist/algoliasearch-lite.umd.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/instantsearch.js@4"></script>
    <script type="text/javascript">
        const search = instantsearch({
          indexName: '@SiteSearchModel.IndexName',
          searchClient: algoliasearch('@algoliaOptions.ApplicationId', '@algoliaOptions.SearchKey'),
        });

        search.addWidgets([
          instantsearch.widgets.searchBox({
            container: '#searchbox',
          }),
          instantsearch.widgets.clearRefinements({
            container: '#clear-refinements',
          }),
          instantsearch.widgets.refinementList({
            sortBy: ['name:asc'],
            container: '#type-list',
            attribute: 'ClassName',
          }),
          instantsearch.widgets.refinementList({
            sortBy: ['name:asc'],
            container: '#processing-list',
            attribute: 'CoffeeProcessing',
          }),
          instantsearch.widgets.refinementList({
            sortBy: ['name:asc'],
            container: '#decaf-list',
            attribute: 'CoffeeIsDecaf',
          }),
          instantsearch.widgets.hits({
            container: '#hits',
            templates: {
              item: renderHitTemplate,
            },
          }),
          instantsearch.widgets.pagination({
            container: '#pagination',
          }),
        ]);

        search.start();

        function renderHitTemplate(item) {
            var template = `<div>
                <a href="${item.Url}">
                    <img src="${item.Thumbnail}" align="left" alt="${item.DocumentName}" />
                </a>
                <div class="hit-name">
                <a href="${item.Url}">
                    ${item.DocumentName}
                </a>
                </div>
                <div class="hit-description">
                ${item.ShortDescription}
                </div>`;
            if (item.SKUPrice > 0) {
                template += `<div class="hit-price">\$${item.SKUPrice}</div>`;
            }
            template += `</div>`;
            return template;
        }
    </script>
}
```

4. Create the _/wwwroot/Content/Styles/instantsearch.css_ stylesheet which overrides some default _InstantSearch.js_ styling to fit the Dancing Goat theme:

```css
.instantsearch-container {
    padding: 20px;
}

.ais-ClearRefinements {
    margin: 1em 0;
}

.ais-SearchBox {
    margin: 1em 0;
    width:  97%;
}

.ais-Pagination {
    margin-top: 1em;
}

.ais-Pagination-item--selected .ais-Pagination-link {
    color: #fff;
    background-color: #272219;
    border-color: #272219;
}

.left-panel {
    float: left;
    width: 290px;
}

.right-panel {
    margin-left: 310px;
}

.ais-InstantSearch {
    max-width: 960px;
    overflow: hidden;
    margin: 0 auto;
}

.ais-RefinementList-count {
    color: #fff;
    background-color: #272219;
}

.ais-ClearRefinements-button {
    background-color: #272219;
}

.ais-ClearRefinements-button--disabled:focus, .ais-ClearRefinements-button--disabled:hover {
    background-color: #272219;
    opacity: 0.9;
}

.ais-Hits-item {
    background-color: #fff;
    margin-bottom: 1em;
    width: calc(50% - 1rem);
    border-radius: 5px;
    border: 0px;
}

.ais-Hits-item img {
    margin-right: 1em;
    width: 100px;
}

.hit-name {
    margin-bottom: 0.5em;
}

.hit-description {
    color: #888;
    font-size: 14px;
    margin-bottom: 0.5em;
}
```

When you run the site and visit your new page, you'll see that you have a fully functioning search interface with faceting. See Algolia's [InstantSearch documentation](https://www.algolia.com/doc/guides/building-search-ui/what-is-instantsearch/js/) for more detailed walkthroughs on designing the search interface and customizing widgets.

![InstantSearch example](/img/instantsearch-example.png)

## :chart_with_upwards_trend: _Algolia search_ application for administration interface

While the Xperience Algolia integration works without any changes to the Xperience administration interface, you may choose to import a custom module into your Xperience website to improve your user's experience.

### Importing the custom module

1.  Download the _Kentico.Xperience.AlgoliaSearch_ ZIP package by locating the latest "Custom module" [Release](https://github.com/Kentico/xperience-algolia/releases).
1. In the Xperience adminstration, open the __Sites__ application.
1. [Import](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects) the downloaded package with the __Import files__ and __Import code files__ [settings](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects#Importingasiteorobjects-Import-Objectselectionsettings) enabled.
1. Perform the [necessary steps](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects#Importingasiteorobjects-Importingpackageswithfiles) to include the following imported folder in your project:
   - `/CMSModules/Kentico.Xperience.AlgoliaSearch`

### Enabling indexing

The imported module includes a setting under __Settings > Integration > Algolia search__ which allows you to enable/disable the indexing of your pages after they are created, updated, or deleted. Make sure to check that this setting is enabled after importing the module.

Indexing can also be disabled through App Settings by setting `AlgoliaSearchDisableIndexing` to `true`:

```xml
<add key="AlgoliaSearchDisableIndexing" value="true" />
```

### Algolia search application

The __Algolia search__ application provides a listing of all registered Algolia search model code files, along with some statistics directly from Algolia. By default, Algolia indexes are not rebuilt at any point - only updated and newly-created pages are indexed. To rebuild the index completely, use the circular arrow icon at the left of the grid.

![Algolia module grid](/img/index-grid.png)

To view details about an index, click the eye icon:

![Algolia index content](/img/index-content.png)

Switch to the __Search preview__ tab to perform a basic Algolia query:

![Algolia index preview](/img/index-preview.png)

This view displays the `objectID` and `ClassName` attributes, plus any other searchable attributes which contained the matching search term.

## Questions & Support

See the [Kentico home repository](https://github.com/Kentico/Home/blob/master/README.md) for more information about the product(s) and general advice on submitting questions.
