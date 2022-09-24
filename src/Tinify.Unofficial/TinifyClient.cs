using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Tinify.Unofficial
{
    using Method = HttpMethod;

    public sealed class TinifyClient : IDisposable
    {
        internal sealed class ErrorData
        {
            [JsonPropertyName("message")]
            public string Message { get; init; }
            [JsonPropertyName("error")]
            public string Error { get; init; }
        }

        private static readonly Uri ShrinkUri = new("/shrink", UriKind.Relative);
        
        private static readonly Uri ApiEndpoint = new("https://api.tinify.com");

        public static readonly short RetryCount = 1;
        public static ushort RetryDelay { get; internal set; }= 500;

        private readonly HttpClient _client;

        /// <summary>
        /// Creates a new Client object for accessing the Tinify API
        /// </summary>
        /// <param name="key">Your Tinify API key</param>
        /// <param name="handler">The message handler responsible for processing HTTP messages.
        /// Can be used to provide a proxy, to add special SSL handling, etc.</param>
        /// <param name="disposeHandler">true if the inner handler should be disposed of by
        /// Client.Dispose; false if you intend to reuse the handler.</param>
        public TinifyClient(string key, HttpMessageHandler handler = null, bool disposeHandler = false)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key), "You must provide a Tinify API key.");
            _client = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler);
            _client.BaseAddress = ApiEndpoint;
            _client.Timeout = Timeout.InfiniteTimeSpan;

            _client.DefaultRequestHeaders.Add("User-Agent", Internal.Platform.UserAgent);

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{key}"));
            _client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
        }

        public async Task<Source> ShrinkFromFile(string path)
        {
            var buffer = await System.IO.File.ReadAllBytesAsync(path).ConfigureAwait(false);
            return await ShrinkFromBuffer(buffer).ConfigureAwait(false);
        }

        public async Task<Source> ShrinkFromBuffer(byte[] buffer)
        {
            var response = await Request(Method.Post, ShrinkUri, buffer).ConfigureAwait(false);
            var location = response.Headers.Location;

            return new Source(location, this);
        }

        public async Task<Source> ShrinkFromUrl(string url)
        {
            var body = new StringContent($"{{\"source\":{{\"url\":\"{url}\"}}}}",
                Encoding.UTF8, "application/json");
            var response = await Request(Method.Post, ShrinkUri, body);
            var location = response.Headers.Location;

            return new Source(location, this);
        }

        public Task<HttpResponseMessage> Request(Method method, string url)
        {
            return Request(method, new Uri(url, UriKind.Relative));
        }

        public Task<HttpResponseMessage> Request(Method method, string url, byte[] body)
        {
            return Request(method, new Uri(url, UriKind.Relative), body);
        }

        public Task<HttpResponseMessage> Request(Method method, string url, Dictionary<string, object> options)
        {
            return Request(method, new Uri(url, UriKind.Relative), options);
        }

        public Task<HttpResponseMessage> Request(Method method, Uri url, byte[] body)
        {
            return Request(method, url, new ByteArrayContent(body));
        }

        public Task<HttpResponseMessage> Request(Method method, Uri url, Dictionary<string, object> options)
        {
            if (method == HttpMethod.Get && options.Count == 0)
            {
                return Request(method, url);
            }

            var json = JsonSerializer.Serialize(options);
            var body = new StringContent(json, Encoding.UTF8, "application/json");
            return Request(method, url, body);
        }

        public async Task<HttpResponseMessage> Request(Method method, Uri url, HttpContent body = null)
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
                    var compressionCount = response.Headers.GetValues("Compression-Count").First();
                    if (uint.TryParse(compressionCount, out var parsed))
                    {
                        Tinify.CompressionCount = parsed;
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
                           new ErrorData() {Message = "Response content was empty.", Error = "ParseError"};
                }
                catch (Exception err)
                {
                    data = new ErrorData
                    {
                        Message = "Error while parsing response: " + err.Message,
                        Error = "ParseError"
                    };
                }
                throw TinifyException.Create(data.Message, data.Error, response.StatusCode);
            }

            return null;
        }
        
        public async Task<bool> Validate()
        {
            try
            {
                await Request(Method.Post, "/shrink").ConfigureAwait(false);
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
        
        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
