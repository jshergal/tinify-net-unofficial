#nullable enable
using System;
using System.Text.Json.Serialization;

namespace Tinify.Unofficial
{
    public record TransformOperations
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global
        [JsonPropertyName("preserve")] public string[]? Preserve { get; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global
        [JsonPropertyName("resize")] public ResizeOperation? Resize { get; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global
        [JsonPropertyName("store")] public object? CloudStore { get; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global
        [JsonPropertyName("convert")] public ConvertOperation? Convert { get; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global MemberCanBePrivate.Global
        [JsonPropertyName("transform")] public BackgroundTransformOperation? Background { get; }

        public TransformOperations(ResizeOperation? resize = null, ConvertOperation? convert = null,
            PreserveOperation? preserve = null, CloudStoreOperation? cloud = null)
        {
            if (resize is null && convert is null && preserve is null && cloud is null)
            {
                throw new ArgumentException("At least one transform operation must be specified");
            }

            Convert = convert;
            Background = convert?.BackgroundTransform;
            Preserve = preserve?.Options;
            Resize = resize;
            CloudStore = cloud;
        }
    }
}
#nullable restore