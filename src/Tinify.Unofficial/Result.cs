using System.IO;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using CommunityToolkit.HighPerformance.Buffers;
using IMemoryOwnerExtensions = CommunityToolkit.HighPerformance.IMemoryOwnerExtensions;

#nullable enable
namespace Tinify.Unofficial
{
    public sealed class Result : IDisposable, IAsyncDisposable
    {
        private MemoryOwner<byte>? _data;
        public int? Width { get; private init; }
        public int? Height { get; private init; }
        public Uri? Location { get; private init; }

        internal static async Task<Result> Create(HttpResponseMessage response, bool disposeResponse = false)
        {
            try
            {
                var header = response.Headers;
                var content = response.Content;
                var contentHeaders = content?.Headers;
                var contentLength = (int?) contentHeaders?.ContentLength;
                return new Result()
                {
                    Width = GetIntValueFromHeader(header, "Image-Width"),
                    Height = GetIntValueFromHeader(header, "Image-Height"),
                    Location = header?.Location,
                    ContentType = contentHeaders?.ContentType?.MediaType,
                    Size = contentHeaders?.ContentLength,
                    _data = await GetDataFromResponse(response, contentLength).ConfigureAwait(false),
                };
            }
            finally
            {
                if (disposeResponse) response.Dispose();
            }
        }

        private Result()
        {
        }

        private static int? GetIntValueFromHeader(HttpHeaders? headers, string value)
        {
            if (headers is null) return null;
            if (!headers.TryGetValues(value, out var values)) return null;
            foreach (var header in values)
                if (int.TryParse(header, out var result))
                    return result;
            return null;
        }

        private static async Task<MemoryOwner<byte>> GetDataFromResponse(HttpResponseMessage response,
            int? contentLength)
        {
            // In .net 6.0 response.Content is never null
#if NETSTANDARD2_1
            if (response.Content is null) return MemoryOwner<byte>.Empty;
#endif
            if (contentLength is null or 0) return MemoryOwner<byte>.Empty;

            var buffer = MemoryOwner<byte>.Allocate(contentLength.Value);
            await response.Content.CopyToAsync(IMemoryOwnerExtensions.AsStream(buffer)).ConfigureAwait(false);
            return buffer;
        }

        public async Task ToFileAsync(string path)
        {
            await using var fs = File.OpenWrite(path);
            await fs.WriteAsync(_data!.Memory);
        }

        public async Task ToStreamAsync(Stream destination) =>
            await destination.WriteAsync(_data!.Memory).ConfigureAwait(false);

        public byte[] ToBuffer() => _data!.Span.ToArray();

        public void CopyToBuffer(Span<byte> buffer) => _data!.Span.CopyTo(buffer);

        public int DataLength => _data!.Length;

        public long? Size { get; private init; }

        public string? ContentType { get; private init; }

        public void Dispose() => DisposeCore();

        private void DisposeCore()
        {
            if (_data is null) return;

            _data.Dispose();
            _data = null;
        }

        public async ValueTask DisposeAsync() => await Task.Run(DisposeCore).ConfigureAwait(false);
    }
}