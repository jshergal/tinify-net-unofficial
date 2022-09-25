using NUnit.Framework;

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;

// ReSharper disable InconsistentNaming

namespace Tinify.Unofficial.Tests
{
    [SetUpFixture]
    public class TinifyUnofficialTestsSetup
    {
        [OneTimeSetUp]
        public void InitializationForAllTests()
        {
            TinifyClient.RetryDelay = 10;
        }
    }
    
    [TestFixture]
    public class Tinify_Key
    {
        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
        }
        
        [Test]
        public void Should_ResetClient_WithNewKey()
        {
            const string key = "fghij";
            using var client = new TinifyClient(key, Helper.MockHandler);

            Helper.EnqueueShrink();
            client.Request(HttpMethod.Get, "/shrink").Wait();

            Assert.AreEqual(
                "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{key}")),
                Helper.LastRequest.Headers.Authorization?.ToString()
            );
        }
    }

    [TestFixture]
    public class Tinify_Client
    {
        [Test]
        public void WithKey_Should_ReturnClient()
        {
            using var client = new TinifyClient("abcde");
            Assert.IsInstanceOf<TinifyClient>(client);
        }

        [Test]
        public void WithNullKey_Should_ThrowException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var _ = new TinifyClient(null);
            });
        }
        
        [Test]
        public void WithEmptyKey_Should_ThrowException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var _ = new TinifyClient(string.Empty);
            });
        }
        
        [Test]
        public void WithWhiteSpaceKey_Should_ThrowException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var _ = new TinifyClient("    \t    ");
            });
        }
    }

    [TestFixture]
    public class Tinify_Validate
    {
        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
        }

        [Test]
        public void WithValidKey_Should_ReturnTrue()
        {
            const string key = "valid";
            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                HttpStatusCode.BadRequest,
                new StringContent("{\"error\":\"Input missing\",\"message\":\"No input\"}")
            );
            
            using var client = new TinifyClient(key, Helper.MockHandler);


            Assert.AreEqual(true, client.Validate().Result);
        }

        [Test]
        public void WithLimitedKey_Should_ReturnTrue()
        {
            const string key = "valid";
            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                HttpStatusCode.TooManyRequests,
                new StringContent("{\"error\":\"Too may requests\",\"message\":\"Your monthly limit has been exceeded\"}")
            );
            using var client = new TinifyClient(key, Helper.MockHandler);


            Assert.AreEqual(true, client.Validate().Result);
        }

        [Test]
        public void WithError_Should_ThrowException()
        {
            const string key = "valid";
            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                HttpStatusCode.Unauthorized,
                new StringContent("{\"error\":\"Unauthorized\",\"message\":\"Credentials are invalid\"}")
            );
            using var client = new TinifyClient(key, Helper.MockHandler);

            Assert.ThrowsAsync<AccountException>(async () =>
            {
                await client.Validate();
            });
        }
    }

    [TestFixture]
    public class Tinify_ShrinkFromSource
    {
        private TinifyClient _client;
        
        [OneTimeSetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            _client = new TinifyClient("valid", Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/some/location");
                return res;
            });
        }

        [OneTimeTearDown]
        public void TearDown() => _client?.Dispose();

        [Test]
        public void FromBuffer_Should_ReturnSourceTask()
        {
            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.IsInstanceOf<Task<Source>>(_client.ShrinkFromBuffer(buffer));
        }
        
        [Test]
        public void FromFile_Should_ReturnSourceTask()
        {
            Assert.IsInstanceOf<Task<Source>>(
                _client.ShrinkFromFile(AppContext.BaseDirectory + "/examples/dummy.png")
            );
        }
        
        [Test]
        public void FromUrl_Should_ReturnSourceTask()
        {
            Assert.IsInstanceOf<Task<Source>>(
                _client.ShrinkFromUrl(Helper.HttpsExampleComTestJpg)
            );
        }
    }
}
