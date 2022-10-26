using System.Text.Json.Serialization;

namespace Tinify.Unofficial
{
    public sealed record GoogleCloudStoreOperation : CloudStoreOperation
    {
        [JsonPropertyName("service")] public override string Service { get; } = "gcs";

        [JsonPropertyName("gcp_access_token")] public string GcpAccessToken { get; init; }

        [JsonPropertyName("path")] public string Path { get; init; }
    }
}