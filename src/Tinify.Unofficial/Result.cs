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
            _data = data;
        }

        public async Task ToFile(string path) => await File.WriteAllBytesAsync(path, _data);

        public async Task ToStream(Stream destination) => await destination.WriteAsync(_data);

        public byte[] ToBuffer() => _data;

        public ulong? Size => (ulong?) _content.ContentLength;

        public string ContentType => _content.ContentType?.MediaType;
    }
}
