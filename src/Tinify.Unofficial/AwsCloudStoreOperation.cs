using System.Text.Json.Serialization;

namespace Tinify.Unofficial
{
    public sealed record AwsCloudStoreOperation : CloudStoreOperation
    {
        [JsonPropertyName("service")] public override string Service { get; } = "s3";

        [JsonPropertyName("aws_access_key_id")]
        public string AwsAccessKeyId { get; init; }

        [JsonPropertyName("aws_secret_access_key")]
        public string AwsSecretAccessKey { get; init; }

        [JsonPropertyName("region")]
        public string Region { get; init; }

        [JsonPropertyName("path")]
        public string Path { get; init; }
    }
}