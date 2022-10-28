using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

#nullable enable

namespace Tinify.Unofficial
{
    public sealed class OptimizedImage : IDisposable, IAsyncDisposable
    {
        private TinifyClient _client = null!;

        private bool _disposed;

        private ImageResult? _result;

        private OptimizedImage()
        {
        }

        public Uri? Location { get; private init; }
        public int? ImageSize { get; private init; }

        public string? ImageType { get; private init; }

        public async ValueTask DisposeAsync() => await Task.Run(DisposeCore).ConfigureAwait(false);

        public void Dispose() => DisposeCore();

        internal static async Task<OptimizedImage> CreateAsync(HttpResponseMessage response, TinifyClient client,
            bool disposeResponse)
        {
            try
            {
                var (imageType, imageSize) = await GetImageDataFromResponse(response).ConfigureAwait(false);
                return new OptimizedImage
                {
                    _client = client,
                    Location = response.Headers.Location,
                    ImageSize = imageSize,
                    ImageType = imageType,
                };
            }
            finally
            {
                if (disposeResponse) response.Dispose();
            }
        }

        private static async Task<(string? imageType, int? imageSize)> GetImageDataFromResponse(
            HttpResponseMessage response)
        {
#if NETSTANDARD2_1
            if (response.Content is null) return (null, null);
#endif
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(body)) return (null, null);

            int? imageSize = null;
            string? imageType = null;
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("output", out var output))
            {
                if (output.TryGetProperty("size", out var size))
                {
                    imageSize = size.GetInt32();
                }

                if (output.TryGetProperty("type", out var imgType))
                {
                    imageType = imgType.GetString();
                }
            }

            return (imageType, imageSize);
        }

        private async ValueTask<ImageResult> GetResult()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(OptimizedImage));
            
            if (_result is not null) return _result;

            _result = await _client.GetResult(this).ConfigureAwait(false);
            return _result;
        }

        public async Task ToFileAsync(string fileName)
        {
            var result = await GetResult();
            await result.ToFileAsync(fileName).ConfigureAwait(false);
        }

        public async Task<byte[]> ToBufferAsync()
        {
            var result = await GetResult();
            return result.ToBuffer();
        }

        public async Task ToStreamAsync(Stream stream)
        {
            var result = await GetResult();
            await result.ToStreamAsync(stream).ConfigureAwait(false);
        }

        public async Task CopyToBufferAsync(Memory<byte> buffer)
        {
            var result = await GetResult();
            result.CopyToBuffer(buffer.Span);
        }

        public async Task<ImageResult> TransformImage(TransformOperations operations) =>
            await _client.GetResult(this, operations).ConfigureAwait(false);

        private void DisposeCore()
        {
            _disposed = true;
            if (_result is null) return;

            _result.Dispose();
            _result = null;
        }
    }
}