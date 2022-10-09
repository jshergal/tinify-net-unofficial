using System.Text.Json.Serialization;

namespace Tinify.Unofficial
{
#pragma warning disable SYSLIB1037
    public sealed record GoogleCloudStoreData : CloudStoreData
    {
        [JsonPropertyName("service")] public string Service { get; } = "gcs";

        [JsonPropertyName("gcp_access_token")] public string GcpAccessToken { get; init; }

        [JsonPropertyName("path")] public string Path { get; init; }
    }
#pragma warning restore SYSLIB1037
}