using System;
using System.Net.Http;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Tinify.Unofficial
{
    using Method = HttpMethod;

    public sealed class Source
    {
        private readonly Uri _url;
        private readonly Dictionary<string, object> _commands;
        private readonly TinifyClient _client;

        internal Source(Uri url, TinifyClient client, Dictionary<string, object> commands = null)
        {
            _url = url;
            _client = client;
            commands ??= new Dictionary<string, object>();
            _commands = commands;
        }

        public Source Preserve(params string[] options)
        {
            return new Source(_url, _client, MergeCommands("preserve", options));
        }

        public Source Resize(object options)
        {
            return new Source(_url, _client, MergeCommands("resize", options));
        }

        public async Task<ResultMeta> Store(object options)
        {
            var response = await _client.Request(Method.Post, _url,
                MergeCommands("store", options)).ConfigureAwait(false);

            return new ResultMeta(response.Headers);
        }

        public async Task<Result> GetResult()
        {
            HttpResponseMessage response;
            if (_commands.Count == 0) {
                response = await _client.Request(Method.Get, _url).ConfigureAwait(false);
            } else {
                response = await _client.Request(Method.Post, _url, _commands).ConfigureAwait(false);
            }

            var body = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            return new Result(response.Headers, response.Content.Headers, body);
        }

        public async Task ToFile(string path) {
            await GetResult().ToFile(path).ConfigureAwait(false);
        }

        public async Task<byte[]> ToBuffer()  {
            return await GetResult().ToBuffer().ConfigureAwait(false);
        }

        private Dictionary<string, object> MergeCommands(string key, object options)
        {
            return new Dictionary<string, object>(this._commands) {{key, options}};
        }
    }
}
