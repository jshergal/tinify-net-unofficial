using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

#nullable enable
namespace Tinify.Unofficial
{
    public sealed record ResizeOperation
    {
        public ResizeOperation(ResizeType resizeType, int? width = null, int? height = null)
        {
            if (resizeType != ResizeType.Scale && (!width.HasValue || !height.HasValue))
                throw new ArgumentException(
                    $"Resize Type: '{resizeType:G}' requires both width and height to be specified");

            if (resizeType == ResizeType.Scale && width.HasValue && height.HasValue)
                throw new ArgumentException(
                    $"You must specify either a width or a height but not both when using Resize Type: '{resizeType:G}'");

            Method = ResizeTypeToString(resizeType);

            Width = width;
            Height = height;
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global
        [JsonPropertyName("method")] public string Method { get; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global
        [JsonPropertyName("width")] public int? Width { get; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global
        [JsonPropertyName("height")] public int? Height { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ResizeTypeToString(ResizeType resizeType)
        {
            return resizeType switch
            {
                ResizeType.Cover => "cover",
                ResizeType.Fit => "fit",
                ResizeType.Scale => "scale",
                ResizeType.Thumb => "thumb",
                _ => throw new ArgumentException("Invalid enum value", nameof(resizeType))
            };
        }
    }

    public enum ResizeType
    {
        Scale,
        Fit,
        Cover,
        Thumb
    }
}
#nullable restore