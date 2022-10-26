using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Tinify.Unofficial
{
    public record TransformOperations
    {
        [JsonPropertyName("preserve")] public string[] Preserve { get; }

        [JsonPropertyName("resize")] public ResizeOperation Resize { get; }

        [JsonPropertyName("store")] public object CloudStore { get; }

        public TransformOperations(PreserveOperation preserveOperation) : this(null, preserveOperation)
        {
        }

        public TransformOperations(CloudStoreOperation cloudOperation) : this(null, cloudOperation: cloudOperation)
        {
        }

        public TransformOperations(ResizeOperation resizeOperation,
            PreserveOperation preserveOperation = null, CloudStoreOperation cloudOperation = null)
        {
            if (resizeOperation is null && preserveOperation is null && cloudOperation is null)
            {
                throw new ArgumentException("At least one transform operation must be specified");
            }

            Preserve = preserveOperation?.Options;
            Resize = resizeOperation;
            CloudStore = cloudOperation;
        }
    }

    [Flags]
    public enum PreserveOptions
    {
        None = 0,
        Copyright = 1 << 0,
        Creation = 1 << 1,
        Location = 1 << 2,
    }

    public sealed record PreserveOperation
    {
        internal string[] Options { get; }

        public PreserveOperation(PreserveOptions options)
        {
            var list = new List<string>(3);
            if (options.HasFlag(PreserveOptions.Copyright)) list.Add("copyright");
            if (options.HasFlag(PreserveOptions.Creation)) list.Add("creation");
            if (options.HasFlag(PreserveOptions.Location)) list.Add("location");
            Options = list.ToArray();
        }
    }

    public enum ResizeType
    {
        Scale,
        Fit,
        Cover,
        Thumb,
    }

    public sealed record ResizeOperation
    {
        [JsonPropertyName("method")] public string Method { get; }

        [JsonPropertyName("width")] public int? Width { get; }

        [JsonPropertyName("height")] public int? Height { get; }

        public ResizeOperation(ResizeType resizeType, int? width = null, int? height = null)
        {
            if (resizeType != ResizeType.Scale && (!width.HasValue || !height.HasValue))
            {
                throw new ArgumentException(
                    $"Resize Type: '{resizeType:G}' requires both width and height to be specified");
            }

            if (resizeType == ResizeType.Scale && width.HasValue && height.HasValue)
            {
                throw new ArgumentException(
                    $"You must specify either a width or a height but not both when using Resize Type: '{resizeType:G}'");
            }

            Method = ResizeTypeToString(resizeType);

            Width = width;
            Height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ResizeTypeToString(ResizeType resizeType)
        {
            return resizeType switch
            {
                ResizeType.Cover => "cover",
                ResizeType.Fit => "fit",
                ResizeType.Scale => "scale",
                ResizeType.Thumb => "thumb",
                _ => throw new ArgumentException("Invalid enum value", nameof(resizeType)),
            };
        }
    }

    public sealed record CloudStoreHeaders([property: JsonPropertyName("Cache-Control")]
        string CacheControl);

    public abstract record CloudStoreOperation
    {
        [JsonPropertyName("service")] public abstract string Service { get; }
        [JsonPropertyName("headers")] public CloudStoreHeaders Headers { get; init; }
    }
}