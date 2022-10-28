using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance.Buffers;
using RichardSzalay.MockHttp;
using Tinify.Unofficial.Internal;

// ReSharper disable InconsistentNaming

namespace Tinify.Unofficial.Tests
{
    [TestFixture]
    public class Tinify_Client
    {
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
    public class Tinify_Client_Key
    {
        private TinifyClient _defaultTestClient;

        [SetUp]
        public void SetUp()
        {
            TinifyClient.ClearClients();
            _defaultTestClient = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);
        }

        [Test]
        public void Should_Give_Same_Client_With_Same_Key_And_Handler()
        {
            var client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);
            var internalClient = client.GetFieldValue<HttpClient>("_client");
            Assert.AreSame(
                _defaultTestClient.GetFieldValue<HttpClient>("_client"),
                internalClient
            );
        }

        [Test]
        public void Should_Give_Different_Client_With_Same_Key_And_Different_Handler()
        {
            var client = new TinifyClient(Helper.DefaultKey);
            var internalClient = client.GetFieldValue<HttpClient>("_client");
            Assert.AreNotSame(
                _defaultTestClient.GetFieldValue<HttpClient>("_client"),
                internalClient
            );
        }

        [Test]
        public void Should_Give_Different_Client_With_Different_Key_And_Same_Handler()
        {
            var client = new TinifyClient("4242424242", Helper.MockHandler);
            var internalClient = client.GetFieldValue<HttpClient>("_client");
            Assert.AreNotSame(
                _defaultTestClient.GetFieldValue<HttpClient>("_client"),
                internalClient
            );
        }
    }

    [TestFixture]
    public class Tinify_Client_Validate
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

            var client = new TinifyClient(key, Helper.MockHandler);

            Assert.AreEqual(true, client.Validate().Result);
        }

        [Test]
        public void WithLimitedKey_Should_ReturnTrue()
        {
            const string key = "valid";
            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                HttpStatusCode.TooManyRequests,
                new StringContent(
                    "{\"error\":\"Too may requests\",\"message\":\"Your monthly limit has been exceeded\"}")
            );
            var client = new TinifyClient(key, Helper.MockHandler);

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

            var client = new TinifyClient(key, Helper.MockHandler);

            Assert.ThrowsAsync<AccountException>(async () => { await client.Validate(); });
        }
    }

    [TestFixture]
    public class Client_Request_WhenValid
    {
        private TinifyClient Client { get; set; }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);
            Helper.EnqueueShrink();
        }

        [TearDown]
        public void TearDown()
        {
            Helper.MockHandler.VerifyNoOutstandingExpectation();
        }

        [Test]
        public void Should_Contain_Proper_Authorization_Header()
        {
            Client.Validate().Wait();
            Assert.AreEqual(
                "Basic YXBpOmtleQ==",
                Helper.LastRequest.Headers.GetValues("Authorization").FirstOrDefault()
            );
        }

        [Test]
        public void Should_IssueRequest_ToEndpoint()
        {
            using var _ = Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).Result;
            Assert.AreEqual("https://api.tinify.com/shrink", Helper.LastRequest.RequestUri?.ToString());
        }

        [Test]
        public async Task Should_ReturnResponse()
        {
            await using var optimizedImage = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            Assert.AreEqual(
                "https://api.tinify.com/foo.png",
                optimizedImage.Location?.ToString()
            );
        }

        [Test]
        public async Task Should_IssueRequest_WithUserAgent()
        {
            await using var _ = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).ConfigureAwait(false);
            
            Assert.AreEqual(
                Platform.UserAgent,
                string.Join(" ", Helper.LastRequest.Headers.GetValues("User-Agent"))
            );
        }

        [Test]
        public void Should_UpdateCompressionCount()
        {
            using var _ = Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).Result;
            Assert.AreEqual(12, TinifyClient.CompressionCount);
        }
    }

    [TestFixture]
    public class Client_Request_WithTimeout_Once
    {
        private TinifyClient Client { get; set; }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink")
                .Respond(_ => throw new TaskCanceledException());

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/foo.png");
                res.Headers.Add("Compression-Count", "12");
                return res;
            });
        }

        [Test]
        public async Task Should_ReturnResponse()
        {
            await using var response = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            Assert.AreEqual(
                "https://api.tinify.com/foo.png",
                response.Location?.ToString()
            );
        }
    }

    [TestFixture]
    public class Client_Request_WithTimeout_Repeatedly
    {
        private TinifyClient Client { get; set; }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink")
                .Respond(_ => throw new TaskCanceledException());
            Helper.MockHandler.Expect("https://api.tinify.com/shrink")
                .Respond(_ => throw new TaskCanceledException());
        }

        [Test]
        public void Should_ThrowConnectionException()
        {
            var error = Assert.ThrowsAsync<ConnectionException>(async () =>
            {
                await using var _ = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            });

            Assert.AreEqual(
                "Timeout while connecting",
                error?.Message
            );
        }
    }

    [TestFixture]
    public class Client_Request_WithSocketError_Once
    {
        private TinifyClient Client { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink")
                .Respond(_ => throw new HttpRequestException("An error occurred while sending the request"));

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/foo.png");
                res.Headers.Add("Compression-Count", "12");
                return res;
            });
        }

        [Test]
        public async Task Should_ReturnResponse()
        {
            await using var response = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            Assert.AreEqual(
                "https://api.tinify.com/foo.png",
                response.Location?.ToString()
            );
        }
    }

    [TestFixture]
    public class Client_Request_WithSocketError_Repeatedly
    {
        private TinifyClient Client { get; set; }
        private const string ErrorOccurred = "An error occurred while sending the request";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink")
                .Respond(_ => throw new HttpRequestException(ErrorOccurred));
            Helper.MockHandler.Expect("https://api.tinify.com/shrink")
                .Respond(_ => throw new HttpRequestException(ErrorOccurred));
        }

        [Test]
        public void Should_ThrowConnectionException()
        {
            var error = Assert.ThrowsAsync<ConnectionException>(async () =>
            {
                await using var _ = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            });

            Assert.AreEqual(
                "Error while connecting: " + ErrorOccurred,
                error?.Message
            );
        }
    }

    [TestFixture]
    public class Client_Request_WithUnexpectedError_Once
    {
        private TinifyClient Client { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink")
                .Respond(_ => throw new Exception("some error"));

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/foo.png");
                res.Headers.Add("Compression-Count", "12");
                return res;
            });
        }

        [Test]
        public async Task Should_ReturnResponse()
        {
            await using var response = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            Assert.AreEqual(
                "https://api.tinify.com/foo.png",
                response.Location?.ToString()
            );
        }
    }

    [TestFixture]
    public class Client_Request_WithUnexpectedError_Repeatedly
    {
        private TinifyClient Client { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink")
                .Respond(_ => throw new Exception("some error"));
            Helper.MockHandler.Expect("https://api.tinify.com/shrink")
                .Respond(_ => throw new Exception("some error"));
        }

        [Test]
        public void Should_ThrowConnectionException()
        {
            var error = Assert.ThrowsAsync<ConnectionException>(async () =>
            {
                await using var _ = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            });

            Assert.AreEqual(
                "Error while connecting: some error",
                error?.Message
            );
        }
    }

    [TestFixture]
    public class Client_Request_WithServerError_Once
    {
        private TinifyClient Client { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                (HttpStatusCode) 584,
                new StringContent("{\"error\":\"InternalServerError\",\"message\":\"Oops!\"}")
            );

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/foo.png");
                res.Headers.Add("Compression-Count", "12");
                return res;
            });
        }

        [Test]
        public async Task Should_ReturnResponse()
        {
            await using var response = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            Assert.AreEqual(
                "https://api.tinify.com/foo.png",
                response.Location?.ToString()
            );
        }
    }

    [TestFixture]
    public class Client_Request_WithServerError_Repeatedly
    {
        private TinifyClient Client { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                (HttpStatusCode) 584,
                new StringContent("{\"error\":\"InternalServerError\",\"message\":\"Oops!\"}")
            );

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                (HttpStatusCode) 584,
                new StringContent("{\"error\":\"InternalServerError\",\"message\":\"Oops!\"}")
            );
        }

        [Test]
        public void Should_ThrowConnectionException()
        {
            var error = Assert.ThrowsAsync<ServerException>(async () =>
            {
                await using var _ = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            });

            Assert.AreEqual(
                "Oops! (HTTP 584/InternalServerError)",
                error?.Message
            );
        }
    }

    [TestFixture]
    public class Client_Request_WithBadServerResponse_Once
    {
        private TinifyClient Client { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                (HttpStatusCode) 543,
                new StringContent("<!-- this is not json -->")
            );

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/foo.png");
                res.Headers.Add("Compression-Count", "12");
                return res;
            });
        }

        [Test]
        public async Task Should_ReturnResponse()
        {
            await using var response = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            Assert.AreEqual(
                "https://api.tinify.com/foo.png",
                response.Location?.ToString()
            );
        }
    }

    [TestFixture]
    public class Client_Request_WithBadServerResponse_Repeatedly
    {
        private TinifyClient Client { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                (HttpStatusCode) 543,
                new StringContent("<!-- this is not json -->")
            );

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                (HttpStatusCode) 543,
                new StringContent("<!-- this is not json -->")
            );
        }

        [Test]
        public void Should_ThrowConnectionException()
        {
            var error = Assert.ThrowsAsync<ServerException>(async () =>
            {
                await using var _ = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            });

            Assert.AreEqual(
                "Error while parsing response: '<' is an invalid start of a value. Path: " +
                "$ | LineNumber: 0 | BytePositionInLine: 0. (HTTP 543/ParseError)",
                error?.Message
            );
        }
    }

    [TestFixture]
    public class Client_Request_WithClientError
    {
        private TinifyClient Client { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                (HttpStatusCode) 492,
                new StringContent("{\"error\":\"BadRequest\",\"message\":\"Oops!\"}")
            );
        }

        [Test]
        public void Should_ThrowClientException()
        {
            var error = Assert.ThrowsAsync<ClientException>(async () =>
            {
                await using var _ = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            });

            Assert.AreEqual(
                "Oops! (HTTP 492/BadRequest)",
                error?.Message
            );
        }
    }

    [TestFixture]
    public class Client_Request_With_Bad_Credentials
    {
        private TinifyClient Client { get; set; }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            Client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);
            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(
                HttpStatusCode.Unauthorized,
                new StringContent("{\"error\":\"Unauthorized\",\"message\":\"Oops!\"}")
            );
        }

        [Test]
        public void Should_ThrowAccountException()
        {
            var error = Assert.ThrowsAsync<AccountException>(async () =>
            {
                await using var _ = await Client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            });

            Assert.AreEqual(
                "Oops! (HTTP 401/Unauthorized)",
                error?.Message
            );
        }
    }

    [TestFixture]
    public class Client_With_Invalid_Api_Key
    {
        private TinifyClient _client;

        [OneTimeSetUp]
        public void SetUp()
        {
            TinifyClient.ClearClients();
            Helper.ResetMockHandler();
            _client = new TinifyClient("invalid", Helper.MockHandler);

            Helper.MockHandler.When("https://api.tinify.com/shrink").Respond(
                HttpStatusCode.Unauthorized,
                new StringContent("{'error':'Unauthorized','message':'Credentials are invalid'}")
            );
        }

        [Test]
        public void FromFile_Should_Throw_AccountException()
        {
            Assert.ThrowsAsync<AccountException>(async () =>
            {
                await using var _ = await _client.ShrinkFromFileAsync(AppContext.BaseDirectory + "/examples/dummy.png");
            });
        }

        [Test]
        public void FromBuffer_Should_Throw_AccountException()
        {
            Assert.ThrowsAsync<AccountException>(async () =>
            {
                await using var _ = await _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes);
            });
        }

        [Test]
        public void FromUrl_Should_Throw_AccountException()
        {
            Assert.ThrowsAsync<AccountException>(async () =>
            {
                await using var _ = await _client.ShrinkFromUrlAsync(Helper.HttpsExampleComTestJpg);
            });
        }
    }

    [TestFixture]
    public class Client_Shrink_With_Valid_Api_Key
    {
        private const string ExpectedOptimizedContent = "compressed file";
        private const string ExpectedCompressedLocation = "https://api.tinify.com/some/location";
        private static readonly string DummyFilePath = Path.Combine(AppContext.BaseDirectory, "examples", "dummy.png");
        private readonly byte[] ExpectedFileContent = File.ReadAllBytes(DummyFilePath);
        
        private TinifyClient _client;
        private byte[] _lastRequestContent;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TinifyClient.ClearClients();
            Helper.ResetMockHandler();
            _client = new TinifyClient("valid", Helper.MockHandler);
            

            Helper.MockHandler.When("https://api.tinify.com/shrink").Respond(req =>
            {
                _lastRequestContent = req.Content?.ReadAsByteArrayAsync().Result ?? Array.Empty<byte>();
                
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", ExpectedCompressedLocation);
                return res;
            });

            Helper.MockHandler.When(ExpectedCompressedLocation).Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ExpectedOptimizedContent),
                };
                res.Headers.Add("Compression-Count", "1");

                return res;
            });
        }

        [Test]
        public async Task FromFile_Should_Send_FileContent()
        {
            await using var optimized = await _client.ShrinkFromFileAsync(DummyFilePath).ConfigureAwait(false);
            Assert.AreEqual(ExpectedFileContent, _lastRequestContent);
        }
        
        [Test]
        public async Task FromFile_ReturnsOptimized_Image_WithCompressedLocation()
        {
            await using var optimized = await _client.ShrinkFromFileAsync(DummyFilePath).ConfigureAwait(false);

            Assert.AreEqual(ExpectedCompressedLocation, optimized.Location?.AbsoluteUri);
            
            var result = Encoding.UTF8.GetString(await optimized.ToBufferAsync().ConfigureAwait(false));
            Assert.AreEqual(ExpectedOptimizedContent, result);
        }
        
        [Test]
        public void FromFile_Should_Return_ImageLocation_Task()
        {
            Assert.IsInstanceOf<Task<OptimizedImage>>(
                _client.ShrinkFromFileAsync(DummyFilePath)
            );
        }
        
        [Test]
        public async Task FromStream_Should_Send_FileContent()
        {
            await using var optimized = await _client.ShrinkFromStreamAsync(File.OpenRead(DummyFilePath)).ConfigureAwait(false);
            Assert.AreEqual(ExpectedFileContent, _lastRequestContent);
        }
        
        [Test]
        public async Task FromStream_ReturnsOptimized_Image_WithCompressedLocation()
        {
            await using var optimized = await _client.ShrinkFromStreamAsync(File.OpenRead(DummyFilePath)).ConfigureAwait(false);

            Assert.AreEqual(ExpectedCompressedLocation, optimized.Location?.AbsoluteUri);
            
            var result = Encoding.UTF8.GetString(await optimized.ToBufferAsync().ConfigureAwait(false));
            Assert.AreEqual(ExpectedOptimizedContent, result);
        }
        
        [Test]
        public void FromStream_Should_Return_ImageLocation_Task()
        {
            Assert.IsInstanceOf<Task<OptimizedImage>>(
                _client.ShrinkFromStreamAsync(File.OpenRead(DummyFilePath))
            );
        }

        [Test]
        public async Task FromBuffer_Should_Send_BufferContent()
        {
            await using var optimized = await _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).ConfigureAwait(false);
            Assert.AreEqual(Helper.MockPngImageBytes, _lastRequestContent);
        }
        
        [Test]
        public async Task FromBuffer_ReturnsOptimized_Image_WithCompressedLocation()
        {
            await using var optimized = await _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).ConfigureAwait(false);

            Assert.AreEqual(ExpectedCompressedLocation, optimized.Location?.AbsoluteUri);
            
            var result = Encoding.UTF8.GetString(await optimized.ToBufferAsync().ConfigureAwait(false));
            Assert.AreEqual(ExpectedOptimizedContent, result);
        }
        
        [Test]
        public void FromBuffer_Should_Return_ImageLocation_Task()
        {
            Assert.IsInstanceOf<Task<OptimizedImage>>(_client.ShrinkFromBufferAsync(Helper.MockPngImageBytes));
        }

        [Test]
        public async Task FromUrl_ReturnsOptimized_Image_WithCompressedLocation()
        {
            await using var optimized =
                await _client.ShrinkFromUrlAsync(Helper.HttpsExampleComTestJpg).ConfigureAwait(false);

            Assert.AreEqual(ExpectedCompressedLocation, optimized.Location?.AbsoluteUri);
            
            var result = Encoding.UTF8.GetString(await optimized.ToBufferAsync().ConfigureAwait(false));
            Assert.AreEqual(ExpectedOptimizedContent, result);
        }
        
        [Test]
        public void FromUrl_Should_Return_ImageLocation_Task()
        {
            Assert.IsInstanceOf<Task<OptimizedImage>>(
                _client.ShrinkFromUrlAsync(Helper.HttpsExampleComTestJpg)
            );
        }

        [Test]
        public void FromUrl_Should_ThrowException_IfRequestIsNotOk()
        {
            Helper.MockHandler.ResetBackendDefinitions();
            Helper.MockHandler.When("https://api.tinify.com/shrink").Respond(
                HttpStatusCode.BadRequest,
                new StringContent("{'error':'Source not found','message':'Cannot parse URL'}")
            );

            Assert.ThrowsAsync<ClientException>(async () =>
            {
                await using var _ = await _client.ShrinkFromUrlAsync("file://wrong");
            });
        }
    }

    [TestFixture]
    public class Tinify_Client_Operations
    {
        private const string ExpectedContent = "compressed file";
        private TinifyClient _client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TinifyClient.ClearClients();
        }

        [SetUp]
        public void SetUp()
        {
            Helper.ResetMockHandler();
            _client = new TinifyClient("valid", Helper.MockHandler);

            Helper.MockHandler.When("https://api.tinify.com/shrink").Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/some/location");
                return res;
            });

            Helper.MockHandler.When("https://api.tinify.com/some/location").Respond(
                HttpStatusCode.OK,
                new StringContent(ExpectedContent)
            );
        }

        [Test]
        public void Preserve_Should_Set_Proper_Request_Body()
        {
            const string expectedData = "copyrighted file";
            Helper.EnqueueShrinkAndResult(expectedData);

            var imageOperations =
                new TransformOperations(
                    preserve: new PreserveOperation(PreserveOptions.Copyright | PreserveOptions.Location));
            using var optimizedImage = _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).Result;
            using var result = _client.GetResult(optimizedImage, imageOperations).Result;

            Assert.AreEqual(
                Encoding.UTF8.GetBytes(expectedData),
                result.ToBuffer()
            );

            using var document = JsonDocument.Parse(Helper.LastBody);
            var preserveProperty = document.RootElement.GetProperty("preserve");
            Assert.AreEqual(JsonValueKind.Array, preserveProperty.ValueKind);
            Assert.AreEqual(2, preserveProperty.GetArrayLength());
            var tempHash = new HashSet<string>(preserveProperty.EnumerateArray().Select(x => x.GetString()));
            Assert.IsTrue(tempHash.Contains("copyright"));
            Assert.IsTrue(tempHash.Contains("location"));
        }

        [Test]
        public void Client_Should_Set_Multiple_Options()
        {
            const string expectedData = "copyrighted resized file";
            Helper.EnqueueShrinkAndResult(expectedData);

            var imageOperations = new TransformOperations(
                resize: new ResizeOperation(ResizeType.Fit, 100, 60),
                preserve: new PreserveOperation(PreserveOptions.Copyright | PreserveOptions.Location));

            using var optimizedImage = _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).Result;
            using var result = _client.GetResult(optimizedImage, imageOperations).Result;
            Assert.AreEqual(
                Encoding.UTF8.GetBytes(expectedData),
                result.ToBuffer()
            );

            using var document = JsonDocument.Parse(Helper.LastBody);
            var resizeProperty = document.RootElement.GetProperty("resize");
            Assert.AreEqual(100, resizeProperty.GetProperty("width").GetInt32());
            Assert.AreEqual(60, resizeProperty.GetProperty("height").GetInt32());

            var preserveProperty = document.RootElement.GetProperty("preserve");
            Assert.AreEqual(JsonValueKind.Array, preserveProperty.ValueKind);
            Assert.AreEqual(2, preserveProperty.GetArrayLength());
            var tempHash = new HashSet<string>(preserveProperty.EnumerateArray().Select(x => x.GetString()));
            Assert.IsTrue(tempHash.Contains("copyright"));
            Assert.IsTrue(tempHash.Contains("location"));
        }

        [Test]
        public void Resize_Should_Set_Data()
        {
            const string expectedData = "small file";
            Helper.EnqueueShrinkAndResult(expectedData);

            var imageOperations = new TransformOperations(new ResizeOperation(ResizeType.Scale, 400));

            using var optimizedImage = _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).Result;
            using var result = _client.GetResult(optimizedImage, imageOperations).Result;
            Assert.AreEqual(
                Encoding.UTF8.GetBytes(expectedData),
                result.ToBuffer()
            );

            using var document = JsonDocument.Parse(Helper.LastBody);
            var resizeProperty = document.RootElement.GetProperty("resize");
            Assert.AreEqual(400, resizeProperty.GetProperty("width").GetInt32());
        }

        [Test]
        public void Store_Should_Set_Data()
        {
            Helper.EnqueuShrinkAndStore();

            var imageOperations = new TransformOperations(
                cloud: new AwsCloudStoreOperation
                {
                    AwsAccessKeyId = "AccessKeyId",
                    AwsSecretAccessKey = "SecretAccessKey",
                    Region = "us-west-1",
                    Path = "example-bucket/my-images/optimized.jpg",
                });
            using var optimizedImage = _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).Result;
            using var result = _client.GetResult(optimizedImage, imageOperations).Result;

            using var document = JsonDocument.Parse(Helper.LastBody);
            var storeElement = document.RootElement.GetProperty("store");
            var serviceElement = storeElement.GetProperty("service");
            Assert.AreEqual("s3", serviceElement.GetString());
        }

        [Test]
        public async Task Store_Should_Return_Result_With_Location()
        {
            Helper.EnqueuShrinkAndStore();

            var imageOperations = new TransformOperations(
                cloud: new AwsCloudStoreOperation
                {
                    AwsAccessKeyId = "AccessKeyId",
                    AwsSecretAccessKey = "SecretAccessKey",
                    Region = "us-west-1",
                    Path = "example-bucket/my-images/optimized.jpg",
                });
            await using var optimizedImage =
                await _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).ConfigureAwait(false);
            await using var result = await _client.GetResult(optimizedImage, imageOperations).ConfigureAwait(false);
            Assert.AreEqual(
                new Uri("https://bucket.s3.amazonaws.com/example"),
                result.Location
            );
        }

        [Test]
        public async Task ToBuffer_Should_ReturnImageData()
        {
            const string expectedData = "compressed file";
            Helper.EnqueueShrinkAndResult(expectedData);
            await using var optimizedImage =
                await _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).ConfigureAwait(false);
            await using var result = await _client.GetResult(optimizedImage).ConfigureAwait(false);

            Assert.AreEqual(
                Encoding.UTF8.GetBytes(expectedData),
                result.ToBuffer()
            );
        }

        [Test]
        public void CopyToBuffer_Should_ReturnImageData()
        {
            const string expectedData = "compressed file";
            Helper.EnqueueShrinkAndResult(expectedData);
            using var optimizedImage = _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).Result;

            using var result = _client.GetResult(optimizedImage).Result;
            using var buffer = MemoryOwner<byte>.Allocate(result.DataLength);
            result.CopyToBuffer(buffer.Span);

            Assert.IsTrue(
                Encoding.UTF8.GetBytes(expectedData).AsSpan().SequenceEqual(buffer.Span)
            );
        }

        [Test]
        public async Task ToFile_Should_StoreImageData()
        {
            const string expectedData = "compressed file";
            Helper.EnqueueShrinkAndResult(expectedData);

            await using var optimizedImage = await _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).ConfigureAwait(false);
            await using var file = new TempFile();
            await optimizedImage.ToFileAsync(file.Path);
            Assert.AreEqual(
                Encoding.UTF8.GetBytes("compressed file"),
                await File.ReadAllBytesAsync(file.Path)
            );
        }

        [Test]
        public async Task To_Stream_Should_StoreImageData()
        {
            Helper.EnqueueShrinkAndResult("compressed file");
            var expected = Encoding.UTF8.GetBytes("compressed file");

            await using var optimizedImage = await _client.ShrinkFromBufferAsync(Helper.MockPngImageBytes).ConfigureAwait(false);
            await using var result = await _client.GetResult(optimizedImage).ConfigureAwait(false);
            using var ms = new MemoryStream(expected.Length * 2);
            await result.ToStreamAsync(ms).ConfigureAwait(false);
            Assert.AreEqual(expected, ms.ToArray());
        }
    }
}