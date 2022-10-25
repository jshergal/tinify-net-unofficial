using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace Tinify.Unofficial.Tests
{
    [TestFixture]
    public class OptimizedImageTest
    {
        private const string CompressedImageDataBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAIkAAAAVCAMAAABFXj3DAAABI1BMVEUAAAAlMmvhADUlMmvhADUjMmslMmslMmslMmslMmslMmvhADXhADUjMmslMmslMmslMmsmMWslMmslMmslMmslMmvhADUlMmslMmvhADUlMmslMmslMmvhADUlMmslMmvfADYlMmvhADUlMmslMmslMmvhADXhADXhADUlMmvgADUkMmslMmvhADXhADUlMmvhADXhADUlMmslMmvhADXhADUlMmvhADUlMmskMmvhADXkADQlMmvhADUlMmvhADXhADUlMmvhADUlMmvfADblADQlMmslMmslMmslMmslMmvhADUlMmvhADXhADXhADXhADXhADUlMmslMmvhADXhADXhADXhADXjADThADXhADUWNm8XNm8lMmvhADXxADEWNm9SCsFvAAAAXXRSTlMA/PKlDRsiDKbXuaXVDvPwzgjAmUTk456JddzSyZ53ZjkyHBThxry3kklDOB/16ubPyLSSi4eCZl0/LSYE/euwgHFxU1IyKCXorFpJLCAUCPfFe2xXPtuDYAsG8uJS2BmLAAADnklEQVRIx+2U2XoSQRBG/yDDFpAdwiKyyi4JRARCFoFATGJ2dwd9/6dwahmBT701N56LVFd3mDrTXdP4R5QyXsIA7o/i8Y7XjceiZTILoM+DAB4LQ0xcwM4jm7jPttRkmIxGo70wfuezk8E1hy8givlGvohfXJ82GgdgnMRn4MDKA26LMNCsN2mpUI+VoLRi9aa8tq4AXjEZb+xUbFqfw2bXwYyecTgBcHF8uLSoziBa+y8p9aco8dM/za52l9/z4a9PLe5LWatCaIyuFYJTEMOuSUm0xdllxEq8xlsxGQaJMnVsjKfMynMIL5dM/phDDXi2tHlG6w83dvrhCmjz/O1y+b3h/mFSiY5JROSp5hzUCcpWjER0HBeTuqQB4Llp0wXjUJMXapJarpgAxbV0DzjUobUnZPIbb7SCljeA7CpdmTx1I8CDDLfPAERVihykSKn9CZzeTmTaCd6qm9QJpxe4vbFNZuFvUu1IisT5bw7ocLzsUdjBgqej5xk1aZbVZECxhxiFLJh93hI5plPMKEsASNCgATVAjeIxIFu3/frTKYwtElmEy1x67JM9cVEIAShzzHErAIZ8OzQQk0L/fjoFwnzEYN7Ro0/wXgw+UvYK2i6v2cQBYMRdC1RlnQjQM8sAKyzQYxM5nKNsKBukiCildwAyahJQE4vFuafC0xkICa4xkQr7IqCD1JUIqG9C94Q+aX27NBChhxWQFBPtV0VMnoN8N/cEA+l13cLV8VzTHhSBV5smzj+ajP5uIrf5WSWb9Vl0VyaVDZMACvLNdVcmWuSjf7ncBTCxP9/j1em0ATTWT+cKzLqJISYD6cQkbJLaxwjSIKy/CgIRVZSOVfxU7ZCaRSsejlDkr/s9KNDCCzE82KM4KwIID+mZ8ZY8cypvv1Nq2S+ZiZfjPgw0zZlEbDwe8p7M3XExM7hjXSD0RHTTv8iNsdvmcI2apCRLYnoPVgE09YylA7a0QaJyrfdiPOuBm2fTZEuQKFOosPhll7M4hNOllOPkQbW0X5ztVfoCcOhoZZIRE1NNPIit3WwuOh5l0yTQX58+w8aNP5FEL1n7Yx0l1q5YUNQeHprMEbwctaQPON8yhWCTDlGW0xUOhkvXjLDEiPwzlIttomhvUW0v4Ujs1fKSOlPVD46X/u0HSmrbTAqAy/PEwtNHzmOFJM49lNJnYtz50mdp310JxHgn1Ek/Ccx5OVCyAg3mKLwtd0I59OnXPfznD/wEzSay6gyVphUAAAAASUVORK5CYII=";

        private const string CompressedImageLocation = "https://api.tinify.com/output/compressedImageLocation";
        private readonly byte[] _compressedImage = Convert.FromBase64String(CompressedImageDataBase64);
        private TinifyClient _client;
        private OptimizedImage _optimizedImage;

        [OneTimeSetUp]
        public void Init()
        {
            TinifyClient.ClearClients();
            _client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);
        }

        [SetUp]
        public void SetUp()
        {
            const string shrinkBodyResponse =
                "{\"input\":{\"size\":17150,\"type\":\"image/png\"},\"output\":{\"size\":1391,\"type\":\"image/png\",\"width\":137" +
                ",\"height\":21,\"ratio\":0.0811,\"url\":\"" + CompressedImageLocation + "\"}}";
            Helper.ResetMockHandler();

            Helper.MockHandler.Expect("https://api.tinify.com/shrink").Respond(_ =>
            {
                var res = new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(shrinkBodyResponse, Encoding.UTF8, "application/json"),
                };
                res.Headers.Add("Location", CompressedImageLocation);
                res.Headers.Add("Compression-Count", "12");
                res.Content.Headers.Add("Content-Length", shrinkBodyResponse.Length.ToString());
                res.Headers.Date = DateTimeOffset.Now;

                return res;
            });

            Helper.MockHandler.Expect(CompressedImageLocation).Respond(req =>
            {
                Helper.LastRequest = req;

                if (req.Content != null)
                {
                    Helper.LastBody = req.Content.ReadAsStringAsync().Result;
                }

                var res = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ReadOnlyMemoryContent(_compressedImage),
                };

                res.Content.Headers.Add("Content-Type", "image/png");
                res.Content.Headers.Add("Image-Width", "137");
                res.Content.Headers.Add("Image-Height", "21");
                res.Headers.Date = DateTimeOffset.Now;
                res.Headers.Add("Compression-Count", "12");
                res.Content.Headers.ContentLength = _compressedImage.Length;

                return res;
            });

            _optimizedImage = _client.ShrinkFromBuffer(Array.Empty<byte>()).Result;
        }

        [TearDown]
        public void CleanUp()
        {
            _optimizedImage?.Dispose();
        }

        [Test]
        public async Task OptimizedImage_ToBuffer_WritesResult()
        {
            var buffer = await _optimizedImage.ToBufferAsync().ConfigureAwait(false);
            Assert.AreEqual(_compressedImage, buffer);
        }

        [Test]
        public async Task OptimizedImage_ToFile_WritesResult()
        {
            await using var tempFile = new TempFile();
            await _optimizedImage.ToFileAsync(tempFile.Path).ConfigureAwait(false);
            await using var dest = new FileStream(tempFile.Path, FileMode.Open, FileAccess.Read);
            using var expected = new MemoryStream(_compressedImage, false);
            FileAssert.AreEqual(expected, dest);
        }

        [Test]
        public async Task OptimizedImage_ToStream_WritesResult()
        {
            using var dest = new MemoryStream(_compressedImage.Length);
            await _optimizedImage.ToStreamAsync(dest).ConfigureAwait(false);
            using var expected = new MemoryStream(_compressedImage, false);
            FileAssert.AreEqual(expected, dest);
        }

        [Test]
        public async Task OptimizedImage_CopyToBuffer_WritesResult()
        {
            using var dest = MemoryOwner<byte>.Allocate(_compressedImage.Length, AllocationMode.Clear);
            await _optimizedImage.CopyToBufferAsync(dest.Memory).ConfigureAwait(false);
            using var expected = new MemoryStream(_compressedImage, false);
            FileAssert.AreEqual(expected, dest.AsStream());
        }

        [Test]
        public async Task OptimizedImage_BuffersResult()
        {
            var resultValue = _optimizedImage.GetFieldValue<Result>("_result");
            Assert.IsNull(resultValue);
            using var dest = MemoryOwner<byte>.Allocate(_compressedImage.Length, AllocationMode.Clear);
            await _optimizedImage.CopyToBufferAsync(dest.Memory).ConfigureAwait(false);

            resultValue = _optimizedImage.GetFieldValue<Result>("_result");
            Assert.IsNotNull(resultValue);
            dest.Span.Clear();
            resultValue.CopyToBuffer(dest.Span);
            Assert.IsTrue(
                dest.Span.SequenceEqual(_compressedImage)
            );
        }
    }
}