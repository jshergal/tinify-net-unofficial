using System;
using System.IO;
using NUnit.Framework;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tinify.Unofficial.Tests
{
    internal static class Helper
    {
        public const string HttpsExampleComTestJpg = "https://example.com/test.jpg";
        public const string DefaultKey = "key";

        public static readonly byte[] MockPngImageBytes = Encoding.UTF8.GetBytes("png file");

        public static readonly MockHttpMessageHandler MockHandler = new();
        private static HttpRequestMessage _last;

        private static readonly string ExamplesBasePath =
            $"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}examples";

        public static readonly string AwsStoreSchema = File.ReadAllText(Path.Combine(ExamplesBasePath, "AwsStoreSchema.txt"));

        public static readonly string GooglsCloudStoreSchema =
            File.ReadAllText(Path.Combine(ExamplesBasePath, "GoogleCloudStoreSchema.txt"));

        public static readonly string TinifyTransformSchema =
            File.ReadAllText(Path.Combine(ExamplesBasePath, "TinifyTransformSchema.txt"));
        
        public static HttpRequestMessage LastRequest
        {
            get => _last;
            set
            {
                _last?.Dispose();
                _last = value;
            }
        }

        public static string LastBody { get; set; }
        public static HttpMethod LastMethod { get; set; }

        public static void ResetMockHandler()
        {
            MockHandler.ResetBackendDefinitions();
            MockHandler.ResetExpectations();
        }

        public static void EnqueueShrink()
        {
            MockHandler.Expect("https://api.tinify.com/shrink").Respond(req =>
            {
                LastRequest = req;
                if (req.Content != null)
                {
                    LastBody = req.Content.ReadAsStringAsync().Result;
                }

                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/foo.png");
                res.Headers.Add("Compression-Count", "12");
                return res;
            });
        }

        public static void EnqueueShrinkAndResult(string body)
        {
            MockHandler.Expect("https://api.tinify.com/shrink").Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/some/location");
                res.Headers.Add("Compression-Count", "12");
                return res;
            });

            MockHandler.Expect("https://api.tinify.com/some/location").Respond(req =>
            {
                LastRequest = req;
                if (req.Content != null)
                {
                    LastBody = req.Content.ReadAsStringAsync().Result;
                }

                var data = Encoding.UTF8.GetBytes(body);
                var res = new HttpResponseMessage(HttpStatusCode.OK);
                res.Content = new ReadOnlyMemoryContent(data);
                res.Content.Headers.ContentLength = data.Length;
                return res;
            });
        }

        public static void EnqueuShrinkAndStore()
        {
            MockHandler.Expect("https://api.tinify.com/shrink").Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/some/location");
                res.Headers.Add("Compression-Count", "12");
                return res;
            });

            MockHandler.Expect("https://api.tinify.com/some/location").Respond(req =>
            {
                LastRequest = req;
                if (req.Content != null)
                {
                    LastBody = req.Content.ReadAsStringAsync().Result;
                }

                var res = new HttpResponseMessage(HttpStatusCode.OK);
                res.Headers.Add("Location", "https://bucket.s3.amazonaws.com/example");
                return res;
            });
        }

        // Helper method for getting a private field
        // Taken from this SO answer: https://stackoverflow.com/a/46488844 posted by
        // Bruno Zell https://stackoverflow.com/users/5185376/bruno-zell
        public static T GetFieldValue<T>(this object obj, string name)
        {
            // Set the flags so that private and public fields from instances will be found
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var field = obj.GetField(name, bindingFlags);
            return (T) field?.GetValue(obj);
        }

        public static FieldInfo GetField(this object obj, string name, BindingFlags flags) =>
            obj.GetType().GetField(name, flags);
    }
}