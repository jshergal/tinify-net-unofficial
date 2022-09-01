using System;
using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace TinifyAPI.Internal
{
    internal static class Ssl
    {
        public static bool ValidationCallback(HttpRequestMessage req, X509Certificate2 cert, X509Chain chain,
            SslPolicyErrors errors)
        {
            const SslPolicyErrors flags = SslPolicyErrors.RemoteCertificateNotAvailable |
                                          SslPolicyErrors.RemoteCertificateNameMismatch;
            if (errors.HasFlag(flags)) return false;
            using var temp = new X509Chain() {ChainPolicy = Policy};
            return temp.Build(cert);
        }

        private static readonly X509ChainPolicy Policy = CreateSslChainPolicy();

        private static X509ChainPolicy CreateSslChainPolicy()
        {
            const string header = "-----BEGIN CERTIFICATE-----";
            const string footer = "-----END CERTIFICATE-----";

            byte[] buffer = null;
            try
            {
                using var stream = GetBundleStream();
                using var reader = new StreamReader(stream);
                var pem = reader.ReadToEnd();
                var policy = new X509ChainPolicy()
                {
                    VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority
                };

                buffer = ArrayPool<byte>.Shared.Rent(2048);
                var start = pem.IndexOf(header, 0, StringComparison.Ordinal);
                while (start >= 0)
                {
                    start += header.Length;
                    var end = pem.IndexOf(footer, start, StringComparison.Ordinal);
                    if (end < 0) break;

                    Array.Clear(buffer, 0, buffer.Length);
                    while (!Convert.TryFromBase64Chars(pem.AsSpan(start, end - start), buffer, out var count))
                    {
                        var newLength = buffer.Length * 2;
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = ArrayPool<byte>.Shared.Rent(newLength);
                        Array.Clear(buffer, 0, buffer.Length);
                    }

                    var cert = new X509Certificate2(buffer);
                    policy.ExtraStore.Add(cert);

                    // Find next begin marker
                    start = pem.IndexOf(header, start, StringComparison.Ordinal);
                }

                return policy;
            }
            finally
            {
                if (buffer is not null)
                    ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static Stream GetBundleStream() =>
            typeof(Ssl).Assembly.GetManifestResourceStream("Tinify.data.cacert.pem");
    }
}