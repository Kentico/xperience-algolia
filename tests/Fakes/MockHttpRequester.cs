using Algolia.Search.Http;
using Algolia.Search.Models.Common;

using Newtonsoft.Json;

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class MockHttpRequester : IHttpRequester
    {
        public Task<AlgoliaHttpResponse> SendRequestAsync(Request request, int totalTimeout, CancellationToken ct = default)
        {
            var batchResponse = new BatchResponse() {
                ObjectIDs = new string[] { "1" },
                TaskID = 1
            };

            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(streamWriter);
            var serializer = new JsonSerializer();

            serializer.Serialize(jsonWriter, batchResponse);
            jsonWriter.Flush();
            streamWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return Task.FromResult(new AlgoliaHttpResponse()
            {
                HttpStatusCode = 200,
                Body = stream
            });
        }
    }
}
