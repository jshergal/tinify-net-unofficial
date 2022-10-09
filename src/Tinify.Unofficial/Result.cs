using System.Net.Http.Headers;
using System.IO;
using System.Threading.Tasks;
using System;

namespace Tinify.Unofficial
{
    public sealed class Result : ResultMeta
    {
        private readonly HttpContentHeaders _content;
        private readonly byte[] _data;

        internal Result(HttpResponseHeaders meta, HttpContentHeaders content, byte[] data) : base(meta)
        {
            _content = content;
            _data = data ?? Array.Empty<byte>();
        }

        public async Task ToFile(string path) => await File.WriteAllBytesAsync(path, _data);

        public async Task ToStream(Stream destination) => await destination.WriteAsync(_data);

        public byte[] ToBuffer()
        {
            if (_data.Length == 0) return Array.Empty<byte>();
            
            var buffer = new byte[_data.Length];
            Array.Copy(_data, buffer, _data.Length);
            return buffer;
        }

        public void CopyToBuffer(Span<byte> buffer) => _data.AsSpan().CopyTo(buffer);

        public int DataLength => _data.Length;

        public long? Size => _content?.ContentLength;

        public string ContentType => _content?.ContentType?.MediaType;
    }
}
