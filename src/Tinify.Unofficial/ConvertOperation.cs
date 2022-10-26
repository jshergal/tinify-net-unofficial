using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

#nullable enable
namespace Tinify.Unofficial
{
    public sealed record ConvertOperation
    {
        public ConvertOperation(ConvertImageFormat format, Color backgroundColor) : this(
            new[] {ImageFormatToString(format)}, ToHtmlColor(backgroundColor))
        {
        }

        public ConvertOperation(ConvertImageFormat format, string? backgroundColor = null) : this(
            new[] {ImageFormatToString(format)}, backgroundColor)
        {
            ImageFormats = new[] {ImageFormatToString(format)};
            BackgroundTransform =
                backgroundColor is not null ? new BackgroundTransformOperation(backgroundColor) : null;
        }

        public ConvertOperation(IEnumerable<ConvertImageFormat> formats, Color backgroundColor) : this(
            ToImageFormatStringArray(formats), ToHtmlColor(backgroundColor))
        {
        }

        public ConvertOperation(IEnumerable<ConvertImageFormat> formats, string? backgroundColor = null) : this(
            ToImageFormatStringArray(formats), backgroundColor)
        {
        }

        private ConvertOperation(string[] formats, string? backgroundColor = null)
        {
            ImageFormats = formats;
            BackgroundTransform = !string.IsNullOrEmpty(backgroundColor)
                ? new BackgroundTransformOperation(backgroundColor)
                : null;
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global
        [JsonPropertyName("type")] public string[] ImageFormats { get; }

        internal BackgroundTransformOperation? BackgroundTransform { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string[] ToImageFormatStringArray(IEnumerable<ConvertImageFormat> formats)
        {
            return new HashSet<string>(formats.Select(ImageFormatToString)).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ImageFormatToString(ConvertImageFormat format)
        {
            return format switch
            {
                ConvertImageFormat.Jpeg => "image/jpeg",
                ConvertImageFormat.Png => "image/png",
                ConvertImageFormat.WebP => "image/webp",
                _ => throw new ArgumentException($"Invalid image format specified: {format}", nameof(format))
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? ToHtmlColor(Color? backgroundColor)
        {
            if (backgroundColor is null) return null;

            var bgColor = backgroundColor.Value;
            return "#" + bgColor.R.ToString("X2", null)
                       + bgColor.G.ToString("X2", null)
                       + bgColor.B.ToString("X2", null);
        }
    }

    public enum ConvertImageFormat
    {
        Jpeg,
        WebP,
        Png
    }

    // ReSharper disable once NotAccessedPositionalProperty.Global
    public sealed record BackgroundTransformOperation([property: JsonPropertyName("background")]
        string BackgroundColor);
}
#nullable restore