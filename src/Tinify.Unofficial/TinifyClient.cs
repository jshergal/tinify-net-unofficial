using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tinify.Unofficial.Internal;

namespace Tinify.Unofficial
{
    #if NETSTANDARD2_1
    using SocketsHttpHandler = StandardSocketsHttpHandler;
    #endif

    public sealed class TinifyClient
    {
        internal sealed record ErrorData([property: JsonPropertyName("message")]
            string Message, [property: JsonPropertyName("error")] string Error);

        private static readonly Uri ShrinkUri = new("/shrink", UriKind.Relative);
        
        private static readonly Uri ApiEndpoint = new("https://api.tinify.com");

        private const short RetryCount = 1;
        public static ushort RetryDelay { get; internal set; }= 500;

        private static volatile int _compressionCount;
        public static int CompressionCount => _compressionCount;

        private static readonly SocketsHttpHandler SocketHandler = new()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };

        private static readonly Dictionary<string, HttpClient> HttpClients = new();

        private static readonly object Lock = new();
        
        /// <summary>
        /// Clears out and disposes of all HttpClients held by the Tinify client.
        /// </summary>
        internal static void ClearClients()
        {
            lock (Lock)
            {
                foreach (var pair in HttpClients)
                {
                    pair.Value.Dispose();
                }
                HttpClients.Clear();
            }
        }
        
        private readonly HttpClient _client;

        private static HttpClient GetClient(string key, HttpMessageHandler handler)
        {
            var tempHandler = handler ?? SocketHandler;
            var clientKey = key + tempHandler;
            lock (Lock)
            {
                if (HttpClients.TryGetValue(clientKey, out var client)) return client;

                client = new HttpClient(tempHandler, false)
                {
                    BaseAddress = ApiEndpoint,
                    Timeout = Timeout.InfiniteTimeSpan,
                };

                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{key}"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
                client.DefaultRequestHeaders.Add("User-Agent", Platform.UserAgent);

                HttpClients.Add(clientKey, client);

                return client;
            }
        }

        /// <summary>
        /// Creates a new Client object for accessing the Tinify API
        /// </summary>
        /// <param name="key">Your Tinify API key</param>
        /// <param name="handler">The message handler responsible for processing HTTP messages.
        /// Can be used to provide a proxy, to add special SSL handling, etc. Note, this handler
        /// will not be disposed by the TinifyClient</param>
        public TinifyClient(string key, HttpMessageHandler handler = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key), "You must provide a Tinify API key.");

            _client = GetClient(key, handler);
        }

        public async Task<OptimizedImage> ShrinkFromFile(string path)
        {
            var buffer = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            return await ShrinkFromBuffer(buffer).ConfigureAwait(false);
        }

        public async Task<OptimizedImage> ShrinkFromBuffer(byte[] buffer)
        {
            using var response = await Request(HttpMethod.Post, ShrinkUri, new ReadOnlyMemoryContent(buffer))
                .ConfigureAwait(false);
            return await OptimizedImage.CreateAsync(response, this);
        }

        public async Task<OptimizedImage> ShrinkFromUrl(string url)
        {
            var body = new StringContent($"{{\"source\":{{\"url\":\"{url}\"}}}}",
                Encoding.UTF8, "application/json");
            
            var response = await Request(HttpMethod.Post, ShrinkUri, body).ConfigureAwait(false);
            return await OptimizedImage.CreateAsync(response, this, true);
        }

        internal async Task<Result> GetResult(OptimizedImage optimizedImage, TransformOperations operations = null)
        {
            using var response = operations is null
                ? await Request(HttpMethod.Get, optimizedImage.Location).ConfigureAwait(false)
                : await Request(HttpMethod.Post, optimizedImage.Location, operations)
                    .ConfigureAwait(false);

            return await Result.Create(response, true).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> Request(HttpMethod method, Uri url, TransformOperations options)
        {
            var json = JsonSerializer.Serialize(options, TinifyConstants.SerializerOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await Request(method, url, content);
        }
        
        private async Task<HttpResponseMessage> Request(HttpMethod method, Uri url, HttpContent body = null)
        {
            for (var retries = RetryCount; retries >= 0; retries--)
            {
                if (retries < RetryCount)
                {
                    await Task.Delay(RetryDelay);
                }

                var request = new HttpRequestMessage(method, url)
                {
                    Content = body
                };

                HttpResponseMessage response;
                try
                {
                    response = await _client.SendAsync(request).ConfigureAwait(false);
                }
                catch (OperationCanceledException err)
                {
                    if (retries > 0) continue;
                    throw new ConnectionException("Timeout while connecting", err);
                }
                catch (Exception err)
                {
                    if (retries > 0) continue;

                    if (err.InnerException != null)
                    {
                        err = err.InnerException;
                    }

                    throw new ConnectionException("Error while connecting: " + err.Message, err);
                }

                if (response.Headers.Contains("Compression-Count"))
                {
                    var compressionCount = response.Headers.GetValues("Compression-Count").FirstOrDefault();
                    if (int.TryParse(compressionCount, out var parsed))
                    {
                        Interlocked.Exchange(ref _compressionCount, parsed);
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                if (retries > 0 && (uint)response.StatusCode >= 500) continue;

                ErrorData data;
                try
                {
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    data = JsonSerializer.Deserialize<ErrorData>(content) ??
                           new ErrorData("Response content was empty.", "ParseError");
                }
                catch (Exception err)
                {
                    data = new ErrorData(
                        "Error while parsing response: " + err.Message,
                        "ParseError"
                    );
                }
                throw TinifyException.Create(data.Message, data.Error, response.StatusCode);
            }

            return null;
        }
        
        public async Task<bool> Validate()
        {
            try
            {
                await Request(HttpMethod.Post, ShrinkUri).ConfigureAwait(false);
            }
            catch (AccountException err) when (err.Status == HttpStatusCode.TooManyRequests)
            {
                return true;
            }
            catch (ClientException)
            {
                return true;
            }
            return false;
        }
    }
}
