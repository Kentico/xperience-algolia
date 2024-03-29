﻿using System;
using System.Collections.Generic;
using System.Linq;

using Algolia.Search.Models.Common;

using CMS.Core;
using CMS.Helpers;
using CMS.Modules;

using Kentico.Xperience.Algolia.Models;
using Kentico.Xperience.Algolia.Services;

namespace Kentico.Xperience.Algolia.Pages
{
    public partial class AlgoliaSearch_Dashboard : AlgoliaUIPage
    {
        private IAlgoliaClient algoliaClient;


        protected void Page_Load(object sender, EventArgs e)
        {
            ShowTaskCount();
            var siteIndexes = IndexStore.Instance.GetAll().Where(i => !i.SiteNames.Any() || i.SiteNames.Contains(CurrentSiteName));
            if (!siteIndexes.Any())
            {
                ShowInformation("No Algolia indexes registered. See <a target='_blank' href='https://github.com/Kentico/xperience-algolia#gear-creating-and-registering-an-algolia-index'>our instructions</a> to read more about creating and registering Algolia indexes.");
                return;
            }

            algoliaClient = Service.Resolve<IAlgoliaClient>();

            LoadIndexes(siteIndexes);
        }


        private void ShowTaskCount()
        {
            if (!IndexStore.Instance.GetAll().Any())
            {
                return;
            }

            ShowInformation($"Queued Algolia search tasks: <b>{AlgoliaQueueWorker.Current.ItemsInQueue}</b>.");
        }


        private void LoadIndexes(IEnumerable<AlgoliaIndex> indexes)
        {
            var indexesToList = new List<IndicesResponse>();
            var indexStatistics = algoliaClient.GetStatistics();
            foreach (var index in indexes)
            {
                // Find statistics with matching name
                var matchingStatistics = indexStatistics.FirstOrDefault(i => i.Name == index.IndexName);
                if (matchingStatistics != null)
                {
                    indexesToList.Add(matchingStatistics);
                }
                else
                {
                    // The index has not been created in Algolia, list index with blank statistics
                    indexesToList.Add(new IndicesResponse() {
                        Name = index.IndexName,
                        Entries = 0,
                        UpdatedAt = DateTime.MinValue,
                        CreatedAt = DateTime.MinValue,
                        LastBuildTimes = 0,
                        DataSize = 0
                    });
                }
            }

            ugIndexes.OnAction += UgIndexes_OnAction;
            ugIndexes.OnExternalDataBound += UgIndexes_OnExternalDataBound;
            ugIndexes.DataSource = ToDataSet(indexesToList);
            ugIndexes.DataBind();
        }

        private object UgIndexes_OnExternalDataBound(object sender, string sourceName, object parameter)
        {
            switch (sourceName)
            {
                case "size":
                    var size = ValidationHelper.GetLong(parameter, 0);
                    if (size == 0)
                    {
                        return parameter;
                    }

                    return BytesToString(size);
            }

            return parameter;
        }


        private void UgIndexes_OnAction(string actionName, object actionArgument)
        {
            var indexName = ValidationHelper.GetString(actionArgument, String.Empty);
            if (String.IsNullOrEmpty(indexName))
            {
                ShowError("Unable to load index name.");
                return;
            }

            switch (actionName)
            {
                case "rebuild":
                    try
                    {
                        algoliaClient.Rebuild(indexName);
                        ShowInformation("Index is rebuilding.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        LogError(ex.Message, "rebuild");
                    }
                    catch (ArgumentNullException ex)
                    {
                        LogError(ex.Message, "rebuild");
                    }

                    break;
                case "view":
                    var url = ApplicationUrlHelper.GetElementUrl("Kentico.Xperience.AlgoliaSearch", "IndexProperties");
                    url = URLHelper.AddParameterToUrl(url, "indexName", indexName);
                    URLHelper.Redirect(url);

                    break;
            }
        }


        private void LogError(string message, string code)
        {
            ShowError("Error proccessing action. Please check the Event Log for more details.");
            Service.Resolve<IEventLogService>().LogError("AlgoliaModule", code, message);
        }


        private string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}