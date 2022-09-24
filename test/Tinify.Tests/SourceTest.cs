using NUnit.Framework;

using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;
using Tinify.Unofficial;

// ReSharper disable InconsistentNaming

namespace Tinify.Unofficial.Tests
{
    internal sealed class TempFile : IDisposable
    {
        public string Path { get; private set; }

        public TempFile()
        {
            Path = System.IO.Path.GetTempFileName();
        }

        ~TempFile()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing) GC.SuppressFinalize(this);
            try
            {
                if (!string.IsNullOrEmpty(Path)) File.Delete(Path);
            }
            catch
            {
                // ignored
            }

            Path = null;
        }
    }

    [TestFixture]
    public class Source_WithInvalidApiKey
    {
        private TinifyClient _client;
        
        [SetUp]
        public void SetUp()
        {
            TinifyClient.RetryDelay = 10;
            Helper.ResetMockHandler();
            _client = new TinifyClient("invalid", Helper.MockHandler);

            Helper.MockHandler.When("https://api.tinify.com/shrink").Respond(
                HttpStatusCode.Unauthorized,
                new StringContent("{'error':'Unauthorized','message':'Credentials are invalid'}")
            );
        }

        [TearDown]
        public void TearDown() => _client?.Dispose();

        [Test]
        public void FromFile_Should_ThrowAccountException()
        {
            Assert.ThrowsAsync<AccountException>(async () =>
            {
                await _client.ShrinkFromFile(AppContext.BaseDirectory + "/examples/dummy.png");
            });
        }

        [Test]
        public void FromBuffer_Should_ThrowAccountException()
        {
            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.ThrowsAsync<AccountException>(async () =>
            {
                await _client.ShrinkFromBuffer(buffer);
            });
        }

        [Test]
        public void FromUrl_Should_ThrowAccountException()
        {
            Assert.ThrowsAsync<AccountException>(async () =>
            {
                await _client.ShrinkFromUrl("http://example.com/test.jpg");
            });
        }
    }

    [TestFixture]
    public class Source_WithValidApiKey
    {
        private TinifyClient _client;
        
        [SetUp]
        public void SetUp()
        {
            TinifyClient.RetryDelay = 10;
            Helper.ResetMockHandler();
            _client = new TinifyClient("valid", Helper.MockHandler);

            Helper.MockHandler.When("https://api.tinify.com/shrink").Respond(req =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created);
                res.Headers.Add("Location", "https://api.tinify.com/some/location");
                return res;
            });

            Helper.MockHandler.When("https://api.tinify.com/some/location").Respond(
                HttpStatusCode.OK,
                new StringContent("compressed file")
            );
        }
        
        [TearDown]
        public void TearDown() => _client?.Dispose();

        [Test]
        public void FromFile_Should_ReturnSourceTask()
        {
            Assert.IsInstanceOf<Task<Source>>(
                _client.ShrinkFromFile(AppContext.BaseDirectory + "/examples/dummy.png")
            );
        }

        [Test]
        public void FromFile_Should_ReturnSourceTask_WithData()
        {
            Assert.AreEqual(
                Encoding.ASCII.GetBytes("compressed file"),
                _client.ShrinkFromFile(AppContext.BaseDirectory + "/examples/dummy.png").ToBuffer().Result
            );
        }

        [Test]
        public void FromBuffer_Should_ReturnSourceTask()
        {
            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.IsInstanceOf<Task<Source>>(_client.ShrinkFromBuffer(buffer));
        }

        [Test]
        public void FromBuffer_Should_ReturnSourceTask_WithData()
        {
            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.AreEqual(
                Encoding.ASCII.GetBytes("compressed file"),
                _client.ShrinkFromBuffer(buffer).ToBuffer().Result
            );
        }

        [Test]
        public void FromUrl_Should_ReturnSourceTask()
        {
            Assert.IsInstanceOf<Task<Source>>(
                _client.ShrinkFromUrl("http://example.com/test.jpg")
            );
        }

        [Test]
        public void FromUrl_Should_ReturnSourceTask_WithData()
        {
            Assert.AreEqual(
                Encoding.ASCII.GetBytes("compressed file"),
                _client.ShrinkFromUrl("http://example.com/test.jpg").ToBuffer().Result
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
                await _client.ShrinkFromUrl("file://wrong");
            });
        }

        [Test]
        public void GetResult_Should_ReturnResultTask()
        {
            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.IsInstanceOf<Task<Result>>(
                _client.ShrinkFromBuffer(buffer).GetResult()
            );
        }

        [Test]
        public void Preserve_Should_ReturnSourceTask()
        {
            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.IsInstanceOf<Task<Source>>(
                _client.ShrinkFromBuffer(buffer).Preserve("copyright", "location")
            );
        }

        [Test]
        public void Preserve_Should_ReturnSourceTask_WithData()
        {
            Helper.EnqueueShrinkAndResult("copyrighted file");

            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.AreEqual(
                Encoding.ASCII.GetBytes("copyrighted file"),
                _client.ShrinkFromBuffer(buffer).Preserve("copyright", "location").ToBuffer().Result
            );

            Assert.AreEqual(
                "{\"preserve\":[\"copyright\",\"location\"]}",
                Helper.LastBody
            );
        }

        [Test]
        public void Preserve_Should_ReturnSourceTask_WithData_ForArray()
        {
            Helper.EnqueueShrinkAndResult("copyrighted file");

            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.AreEqual(
                Encoding.ASCII.GetBytes("copyrighted file"),
                _client.ShrinkFromBuffer(buffer).Preserve(new string[] {"copyright", "location"}).ToBuffer().Result
            );

            Assert.AreEqual(
                "{\"preserve\":[\"copyright\",\"location\"]}",
                Helper.LastBody
            );
        }

        [Test]
        public void Preserve_Should_IncludeOtherOptions_IfSet()
        {
            Helper.EnqueueShrinkAndResult("copyrighted resized file");

            var resizeOptions = new { width = 100, height = 60 };
            var preserveOptions = new[] {"copyright", "location"};

            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.AreEqual(
                Encoding.ASCII.GetBytes("copyrighted resized file"),
                _client.ShrinkFromBuffer(buffer).Resize(resizeOptions).Preserve(preserveOptions).ToBuffer().Result
            );

            Assert.AreEqual(
                "{\"resize\":{\"width\":100,\"height\":60},\"preserve\":[\"copyright\",\"location\"]}",
                Helper.LastBody
            );
        }

        [Test]
        public void Resize_Should_ReturnSourceTask()
        {
            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.IsInstanceOf<Task<Source>>(
                _client.ShrinkFromBuffer(buffer).Resize(new { width = 400 })
            );
        }

        [Test]
        public void Resize_Should_ReturnSourceTask_WithData()
        {
            Helper.EnqueueShrinkAndResult("small file");

            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.AreEqual(
                Encoding.ASCII.GetBytes("small file"),
                _client.ShrinkFromBuffer(buffer).Resize(new { width = 400 }).ToBuffer().Result
            );

            Assert.AreEqual(
                "{\"resize\":{\"width\":400}}",
                Helper.LastBody
            );
        }

        [Test]
        public void Store_Should_ReturnResultMetaTask()
        {
            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.IsInstanceOf<Task<ResultMeta>>(
                _client.ShrinkFromBuffer(buffer).Store(new { service = "s3" })
            );
        }

        [Test]
        public void Store_Should_ReturnResultMetaTask_WithLocation()
        {
            Helper.EnqueuShrinkAndStore();

            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.AreEqual(
                new Uri("https://bucket.s3.amazonaws.com/example"),
                _client.ShrinkFromBuffer(buffer).Store(new { service = "s3" }).Result.Location
            );

            Assert.AreEqual(
                "{\"store\":{\"service\":\"s3\"}}",
                Helper.LastBody
            );
        }

        [Test]
        public void Store_Should_IncludeOtherOptions_IfSet()
        {
            Helper.EnqueuShrinkAndStore();

            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.AreEqual(
                new Uri("https://bucket.s3.amazonaws.com/example"),
                _client.ShrinkFromBuffer(buffer).Resize(new { width = 400 }).Store(new { service = "s3" }).Result.Location
            );

            Assert.AreEqual(
                "{\"resize\":{\"width\":400},\"store\":{\"service\":\"s3\"}}",
                Helper.LastBody
            );
        }

        [Test]
        public void ToBuffer_Should_ReturnImageData()
        {
            Helper.EnqueueShrinkAndResult("compressed file");

            var buffer = Encoding.ASCII.GetBytes("png file");
            Assert.AreEqual(
                Encoding.ASCII.GetBytes("compressed file"),
                _client.ShrinkFromBuffer(buffer).ToBuffer().Result
            );
        }

        [Test]
        public void ToFile_Should_StoreImageData()
        {
            Helper.EnqueueShrinkAndResult("compressed file");

            var buffer = Encoding.ASCII.GetBytes("png file");
            using (var file = new TempFile())
            {
                _client.ShrinkFromBuffer(buffer).ToFile(file.Path).Wait();
                Assert.AreEqual(
                    Encoding.ASCII.GetBytes("compressed file"),
                    File.ReadAllBytes(file.Path)
                );
            }
        }
    }
}
