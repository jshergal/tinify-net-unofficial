using System;
using System.Linq;
using NUnit.Framework;
using System.Net.Http;
using System.Text;
// ReSharper disable InconsistentNaming

namespace Tinify.Unofficial.Tests
{
    [TestFixture]
    public class Result_NoMeta_NoData
    {
        private Result _result;

        [OneTimeSetUp]
        public void SetUp()
        {
            var response = new HttpResponseMessage();
            _result = Result.Create(response, true).Result;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _result?.Dispose();
        }

        [Test]
        public void Width_Should_ReturnNull()
        {
            Assert.AreEqual(null, _result.Width);
        }

        [Test]
        public void Height_Should_ReturnNull()
        {
            Assert.AreEqual(null, _result.Height);
        }

        [Test]
        public void Location_Should_ReturnImageNull()
        {
            Assert.AreEqual(null, _result.Location);
        }

        [Test]
        public void Buffer_Should_ReturnEmpty()
        {
            Assert.AreEqual(Array.Empty<byte>(), _result.ToBuffer());
        }
    }
    [TestFixture]
    public class Result_With_OnlyMeta
    {
        private const string ExpectedLocation = "https://example.com/image.png";
        private Result _result;
        
        [OneTimeSetUp]
        public void SetUp()
        {
            var response = new HttpResponseMessage();
            var headers = response.Headers;
            headers.Add("Image-Width", "100");
            headers.Add("Image-Height", "60");
            headers.Add("Location", ExpectedLocation);
            _result = Result.Create(response, true).Result;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _result?.Dispose();
        }

        [Test]
        public void Width_Should_ReturnImageWidth()
        {
            Assert.AreEqual(100, _result.Width);
        }

        [Test]
        public void Height_Should_ReturnImageHeight()
        {
            Assert.AreEqual(60, _result.Height);
        }

        [Test]
        public void Location_Should_ReturnImageLocation()
        {
            Assert.AreEqual(new Uri(ExpectedLocation), _result.Location);
        }
    }
    
    [TestFixture]
    public class Result_With_MetaAndData
    {
        private const string PngImageData = "png image data";
        private Result _result;
        private long ExpectedSize;

        [OneTimeSetUp]
        public void Init()
        {
            var data = Encoding.UTF8.GetBytes(PngImageData);
            ExpectedSize = data.Length;
            var response = new HttpResponseMessage()
            {
                Content = new ByteArrayContent(data),
            };

            response.Headers.Add("Image-Width", "100");
            response.Headers.Add("Image-Height", "60");
            response.Content.Headers.Clear();
            response.Content.Headers.Add("Content-Type", "image/png");
            response.Content.Headers.ContentLength = data.Length;
            _result = Result.Create(response, true).Result;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _result?.Dispose();
        }

        [Test]
        public void Width_Should_ReturnImageWidth()
        {
            Assert.AreEqual(100, _result.Width);
        }

        [Test]
        public void Height_Should_ReturnImageHeight()
        {
            Assert.AreEqual(60, _result.Height);
        }

        [Test]
        public void Size_Should_ReturnContentLength()
        {
            Assert.AreEqual(ExpectedSize, _result.Size);
        }

        [Test]
        public void ContentType_Should_ReturnMimeType()
        {
            Assert.AreEqual("image/png", _result.ContentType);
        }

        [Test]
        public void ToBuffer_Should_ReturnImageData()
        {
            Assert.AreEqual(Encoding.UTF8.GetBytes(PngImageData), _result.ToBuffer());
        }

        [Test]
        public void CopyToBuffer_Should_CopyData()
        {
            Span<byte> destination = stackalloc byte[(int)ExpectedSize];
            destination.Clear(); // Ensure the span is zeroed
            _result.CopyToBuffer(destination);
            Assert.IsTrue(destination.SequenceEqual(_result.ToBuffer()));
        }

        [Test]
        public void CopyToBuffer_Exception_Buffer_TooSmall()
        {
            var destination = Array.Empty<byte>();
            Assert.Throws<ArgumentException>(() => _result.CopyToBuffer(destination));
        }
    }

    [TestFixture]
    public class Result_Without_MetaAndData
    {
        private Result _result;

        [OneTimeSetUp]
        public void SetUp()
        {
            var response = new HttpResponseMessage()
            {
                Content = new StringContent(string.Empty),
            };

            response.Content.Headers.Clear();
            response.Content.Headers.ContentLength = null;
            _result = Result.Create(response, true).Result;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _result?.Dispose();
        }

        [Test]
        public void Width_Should_ReturnNull()
        {
            Assert.AreEqual(null, _result.Width);
        }

        [Test]
        public void Height_Should_ReturnNull()
        {
            Assert.AreEqual(null, _result.Height);
        }

        [Test]
        public void Location_Should_ReturnImageNull()
        {
            Assert.AreEqual(null, _result.Location);
        }

        [Test]
        public void Size_Should_ReturnNull()
        {
            Assert.AreEqual(null, _result.Size);
        }

        [Test]
        public void ContentType_Should_ReturnNull()
        {
            Assert.AreEqual(null, _result.ContentType);
        }

        [Test]
        public void ToBuffer_Should_ReturnEmpty()
        {
            Assert.AreEqual(Array.Empty<byte>(), _result.ToBuffer());
        }
    }
}
