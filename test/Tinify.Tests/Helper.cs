using NUnit.Framework;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace Tinify.Unofficial.Tests
{
    internal static class Helper
    {
        private static readonly FieldInfo HttpClientField = typeof(TinifyClient)
            .GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo HttpHandlerField = GetHttpHandlerField();

        private static FieldInfo GetHttpHandlerField()
        {
            var msgInvoker = typeof(HttpMessageInvoker);
            var handlerField = msgInvoker.GetField("handler", BindingFlags.Instance | BindingFlags.NonPublic);
            return handlerField ?? msgInvoker.GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static MockHttpMessageHandler MockHandler = new MockHttpMessageHandler();
        public static HttpRequestMessage LastRequest;
        public static string LastBody;

        public static void ResetMockHandler()
        {
            MockHandler.ResetBackendDefinitions();
            MockHandler.ResetExpectations();
        }

        public static void MockClient(TinifyClient test)
        {
            MockHandler = new MockHttpMessageHandler();

            /* Terrible hack to get/mock/replace client property. */
            var client = (HttpClient) HttpClientField.GetValue(test);
            HttpHandlerField.SetValue(client, MockHandler);

            TinifyClient.RetryDelay = 10;
        }

        public static void EnqueueShrink()
        {
            TinifyClient.RetryDelay = 10;

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
            TinifyClient.RetryDelay = 10;

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
                res.Content = new StringContent(body);
                return res;
            });
        }

        public static void EnqueuShrinkAndStore()
        {
            TinifyClient.RetryDelay = 10;

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

        // Helper method added due to a behavior change in .Net 6.0 where instead of returning null,
        // HttpContent will be of type EmptyContentType
#if NET5_0_OR_GREATER
        private static readonly System.Type EmptyContentType = typeof(HttpContent).Assembly.GetType("System.Net.Http.EmptyContent");

        public static void AssertEmptyResponseContent(HttpContent content) => Assert.IsInstanceOf(EmptyContentType, content);
#else
        public static void AssertEmptyResponseContent(HttpContent content) => Assert.AreEqual(null, content);
#endif
    }
}
