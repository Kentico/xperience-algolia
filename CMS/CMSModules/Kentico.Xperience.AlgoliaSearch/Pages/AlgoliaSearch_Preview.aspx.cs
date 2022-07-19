using Algolia.Search.Models.Search;

using CMS.Core;
using CMS.Helpers;

using Kentico.Xperience.AlgoliaSearch.Services;

using Newtonsoft.Json.Linq;

using System;
using System.Text;
using System.Web;

namespace Kentico.Xperience.AlgoliaSearch.Pages
{
    public partial class AlgoliaSearch_Preview : AlgoliaUIPage
    {
        private string indexName;


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            indexName = QueryHelper.GetString("indexName", String.Empty);
            if (String.IsNullOrEmpty(indexName))
            {
                ShowError("Unable to load index name.");
                searchPnl.Visible = false;
                return;
            }

            if (!RequestHelper.IsPostBack())
            {
                string searchText = QueryHelper.GetString("searchtext", String.Empty);
                if (!String.IsNullOrEmpty(searchText))
                {
                    var searchIndexService = Service.Resolve<IAlgoliaIndexService>();
                    var searchIndex = searchIndexService.InitializeIndex(indexName);
                    if (searchIndex == null)
                    {
                        ShowError("Error loading search index. Please check the Event Log for more details.");
                    }

                    var query = new Query(searchText);
                    var results = searchIndex.Search<JObject>(query);

                    repSearchResults.DataSource = results.Hits;
                    repSearchResults.PagerForceNumberOfResults = results.Hits.Count;
                    repSearchResults.DataBind();

                    if (results.Hits.Count == 0)
                    {
                        lblNoResults.Visible = true;
                    }
                }

                txtSearchFor.Text = QueryHelper.GetString("searchtext", "");
            }
        }


        /// <summary>
        /// Search button click handler.
        /// </summary>
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string url = RequestContext.CurrentURL;
            url = URLHelper.RemoveParameterFromUrl(url, "page");
            url = URLHelper.UpdateParameterInUrl(url, "searchtext", HttpUtility.UrlEncode(txtSearchFor.Text));

            URLHelper.Redirect(url);
        }


        /// <summary>
        /// See <see href="https://www.algolia.com/doc/api-reference/api-methods/search/#response"/> for the
        /// returned hits format.
        /// </summary>
        protected string GetResultString(object jObject)
        {
            var retVal = new StringBuilder();
            var data = jObject as JObject;
            if (data == null)
            {
                ShowError("Error loading results.");
                return "";
            }

            retVal.Append($"<b>objectID</b>: {data.Value<string>("objectID")}<br/>");
            retVal.Append($"<b>ClassName</b>: {data.Value<string>("ClassName")}<br/>");

            var highlightedResults = data.Value<JObject>("_highlightResult");
            foreach (var prop in highlightedResults.Properties())
            {
                if (prop.Value.Value<string>("matchLevel") == "none" || prop.Name == "ClassName")
                {
                    continue;
                }

                var propValue = HTMLHelper.LimitLength(HTMLHelper.StripTags(prop.Value.Value<string>("value"), false), 340, "...", false);
                retVal.Append($"<b>{prop.Name}</b>: {propValue}<br/>");
            }

            return retVal.ToString();
        }
    }
}