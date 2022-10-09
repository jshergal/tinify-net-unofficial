using System.Text.Json.Serialization;

namespace Tinify.Unofficial
{
#pragma warning disable SYSLIB1037
    public sealed record AwsCloudStoreData : CloudStoreData
    {
        [JsonPropertyName("service")] public string Service { get; } = "s3";

        [JsonPropertyName("aws_access_key_id")]
        public string AwsAccessKeyId { get; init; }

        [JsonPropertyName("aws_secret_access_key")]
        public string AwsSecretAccessKey { get; init; }

        [JsonPropertyName("region")]
        public string Region { get; init; }

        [JsonPropertyName("headers")]
        public AwsCloudStoreHeaders Headers { get; init; }

        [JsonPropertyName("path")]
        public string Path { get; init; }
    }

    public sealed record AwsCloudStoreHeaders
    {
        [JsonPropertyName("Cache-Control")]
        public string CacheControl { get; init; }
    }
#pragma warning restore SYSLIB1037
}