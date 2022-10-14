using System;
using System.Collections.Generic;
using System.Linq;

using CMS.Core;

using Kentico.Xperience.Algolia.Models;
using Kentico.Xperience.Algolia.Services;

using NSubstitute;

using NUnit.Framework;

using static Kentico.Xperience.Algolia.Tests.TestSearchModels;

namespace Kentico.Xperience.Algolia.Tests
{
    internal class AlgoliaQueueWorkerTests
    {
        [TestFixture]
        internal class EnqueueAlgoliaQueueItemTests : AlgoliaTests
        {
            private readonly IAlgoliaClient algoliaClient = Substitute.For<IAlgoliaClient>();


            protected override void RegisterTestServices()
            {
                Service.Use<IAlgoliaClient>(algoliaClient);
            }


            [Test]
            public void EnqueueAlgoliaQueueItem_InvalidIndex_ThrowsException_DoesntQueue()
            {
                Assert.Multiple(() => {
                    Assert.Throws<InvalidOperationException>(() => AlgoliaQueueWorker.EnqueueAlgoliaQueueItem(
                        new AlgoliaQueueItem(FakeNodes.ArticleEn, AlgoliaTaskType.CREATE, "FAKE_INDEX")));
                    Assert.That(AlgoliaQueueWorker.Current.ItemsInQueue, Is.EqualTo(0));
                });
            }


            [Test]
            public void EnqueueAlgoliaQueueItem_ValidItems_ProcessesItems()
            {
                var createTask = new AlgoliaQueueItem(FakeNodes.ArticleEn, AlgoliaTaskType.CREATE, nameof(ArticleEnSearchModel));
                var deleteTask = new AlgoliaQueueItem(FakeNodes.ProductEn, AlgoliaTaskType.DELETE, nameof(ProductsSearchModel));
                AlgoliaQueueWorker.EnqueueAlgoliaQueueItem(createTask);
                AlgoliaQueueWorker.EnqueueAlgoliaQueueItem(deleteTask);

                algoliaClient.Received(1).ProcessAlgoliaTasks(
                    Arg.Is<IEnumerable<AlgoliaQueueItem>>(arg => arg.SequenceEqual(new AlgoliaQueueItem[] { createTask })));
                algoliaClient.Received(1).ProcessAlgoliaTasks(
                    Arg.Is<IEnumerable<AlgoliaQueueItem>>(arg => arg.SequenceEqual(new AlgoliaQueueItem[] { deleteTask })));
            }
        }
    }
}
