#nullable enable
using System.Text.Json.Serialization;

namespace Tinify.Unofficial
{
    public abstract record CloudStoreOperation
    {
        [JsonPropertyName("service")] public abstract string Service { get; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        [JsonPropertyName("headers")] public CloudStoreHeaders? Headers { get; init; }
    }

    // ReSharper disable once NotAccessedPositionalProperty.Global
    public sealed record CloudStoreHeaders([property: JsonPropertyName("Cache-Control")]
        string CacheControl);
}