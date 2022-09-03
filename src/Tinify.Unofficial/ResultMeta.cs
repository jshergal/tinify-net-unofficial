using System;
using System.Net.Http.Headers;

namespace Tinify.Unofficial
{
    public class ResultMeta
    {
        internal ResultMeta(HttpResponseHeaders meta)
        {
            Width = GetWidth(meta);
            Height = GetHeight(meta);
            Location = meta.Location;
        }

        public uint? Width { get; }
        public uint? Height { get; }
        public Uri Location { get; }

        private static uint? GetWidth(HttpResponseHeaders meta)
        {
            if (!meta.TryGetValues("Image-Width", out var values)) return null;

            foreach (var header in values)
                if (uint.TryParse(header, out var value))
                    return value;

            return null;
        }

        private static uint? GetHeight(HttpResponseHeaders meta)
        {
            if (!meta.TryGetValues("Image-Height", out var values)) return null;

            foreach (var header in values)
                if (uint.TryParse(header, out var value))
                    return value;

            return null;
        }
    }
}