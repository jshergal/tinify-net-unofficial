using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using NJsonSchema;
using NJsonSchema.Validation;
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
        private int _compressionCount;

        [OneTimeSetUp]
        public void Init()
        {
            TinifyClient.ClearClients();
            _client = new TinifyClient(Helper.DefaultKey, Helper.MockHandler);
            _compressionCount = 1;
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
                    Content = new StringContent(shrinkBodyResponse, Encoding.UTF8, MediaTypeNames.Application.Json),
                };
                res.Headers.Add("Location", CompressedImageLocation);
                res.Headers.Add("Compression-Count", (_compressionCount++).ToString());
                res.Content.Headers.Add("Content-Length", shrinkBodyResponse.Length.ToString());
                res.Headers.Date = DateTimeOffset.Now;

                return res;
            });

            Helper.MockHandler.Expect(CompressedImageLocation).Respond(req =>
            {
                Helper.LastRequest = req;
                Helper.LastMethod = req.Method;

                if (req.Method == HttpMethod.Post) ++_compressionCount;

                if (req.Content is not null)
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
                res.Headers.Add("Compression-Count", _compressionCount.ToString());
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
            var resultField = _optimizedImage.GetField("_result", BindingFlags.NonPublic | BindingFlags.Instance);
            var resultValue = (ImageResult) resultField?.GetValue(_optimizedImage);
            Assert.IsNull(resultValue);

            // Calling any of the To methods on OptimizedImage should force the image data to be buffered within the object
            using var dest = MemoryOwner<byte>.Allocate(_compressedImage.Length, AllocationMode.Clear);
            await _optimizedImage.CopyToBufferAsync(dest.Memory).ConfigureAwait(false);
            Assert.IsTrue(
                dest.Span.SequenceEqual(_compressedImage)
            );

            // Verify that the OptimizedImage is now holding the result value
            resultValue = (ImageResult) resultField?.GetValue(_optimizedImage);
            Assert.IsNotNull(resultValue);

            // Sanity check, copy the buffered result and verify that it is also correct
            dest.Span.Clear();
            resultValue.CopyToBuffer(dest.Span);
            Assert.IsTrue(
                dest.Span.SequenceEqual(_compressedImage)
            );
        }

        [Test]
        public async Task OptimizedImage_TransformResize_FormatsRequest()
        {
            await using var imageResult = await _optimizedImage.TransformImage(new TransformOperations(
                new ResizeOperation(ResizeType.Fit, 150, 100)));
            
            // Transform operations should be sent as POST requests
            Assert.AreEqual(HttpMethod.Post, Helper.LastMethod);
            
            // Verify that the last request body was sent as JSON
            Assert.AreEqual(MediaTypeNames.Application.Json, Helper.LastRequest!.Content!.Headers!.ContentType!.MediaType);
            
            // Body of the last request should include "resize"
            Assert.IsTrue(Helper.LastBody.Contains("resize", StringComparison.Ordinal));
            
            // Verify that the request matches the expected JSON Schema
            var schema = await JsonSchema.FromJsonAsync(Helper.TinifyTransformSchema);
            Assert.AreEqual(0, schema.Validate(Helper.LastBody).Count);
        }
        
        [Test]
        public async Task OptimizedImage_TransformPreserve_FormatsRequest()
        {
            await using var imageResult = await _optimizedImage.TransformImage(new TransformOperations(
                new PreserveOperation(PreserveOptions.Copyright | PreserveOptions.Creation)));
            
            // Transform operations should be sent as POST requests
            Assert.AreEqual(HttpMethod.Post, Helper.LastMethod);
            
            // Verify that the last request body was sent as JSON
            Assert.AreEqual(MediaTypeNames.Application.Json, Helper.LastRequest!.Content!.Headers!.ContentType!.MediaType);
            
            // Body of the last request should include and "preserve" as well as copyright and creation
            Assert.IsTrue(Helper.LastBody.Contains("preserve", StringComparison.Ordinal));
            Assert.IsTrue(Helper.LastBody.Contains("copyright", StringComparison.Ordinal));
            Assert.IsTrue(Helper.LastBody.Contains("creation", StringComparison.Ordinal));
            
            // Verify that the request matches the expected JSON Schema
            var schema = await JsonSchema.FromJsonAsync(Helper.TinifyTransformSchema);
            Assert.AreEqual(0, schema.Validate(Helper.LastBody).Count);
        }
        
        [Test]
        public async Task OptimizedImage_TransformResizeAndPreserve_FormatsRequest()
        {
            await using var imageResult = await _optimizedImage.TransformImage(new TransformOperations(
                new ResizeOperation(ResizeType.Fit, 150, 100),
                new PreserveOperation(PreserveOptions.Copyright | PreserveOptions.Creation)));
            
            // Transform operations should be sent as POST requests
            Assert.AreEqual(HttpMethod.Post, Helper.LastMethod);
            
            // Verify that the last request body was sent as JSON
            Assert.AreEqual(MediaTypeNames.Application.Json, Helper.LastRequest!.Content!.Headers!.ContentType!.MediaType);
            
            // Body of the last request should include both "resize" and "preserve" along with the associated settings
            Assert.IsTrue(Helper.LastBody.Contains("resize", StringComparison.Ordinal));
            Assert.IsTrue(Helper.LastBody.Contains("width", StringComparison.Ordinal));
            Assert.IsTrue(Helper.LastBody.Contains("height", StringComparison.Ordinal));
            Assert.IsTrue(Helper.LastBody.Contains("preserve", StringComparison.Ordinal));
            Assert.IsTrue(Helper.LastBody.Contains("copyright", StringComparison.Ordinal));
            Assert.IsTrue(Helper.LastBody.Contains("creation", StringComparison.Ordinal));
            
            // Verify that the request matches the expected JSON Schema
            var schema = await JsonSchema.FromJsonAsync(Helper.TinifyTransformSchema);
            Assert.AreEqual(0, schema.Validate(Helper.LastBody).Count);
        }

        [Test]
        public async Task OptimizedImage_TransformStoreAws_FormatsRequest()
        {
            var awsStoreOperation = new AwsCloudStoreOperation()
            {
                AwsAccessKeyId = "MY_ACCESS_KEY_ID",
                AwsSecretAccessKey = "MY_SECRET_ACCESS_KEY",
                Path = "my-bucket/my-images/stored.image.jpg",
                Region = "us-east-1",
                Headers = new CloudStoreHeaders("public, max-age=31536000"),
            };
            await using var imageResult =
                await _optimizedImage.TransformImage(new TransformOperations(awsStoreOperation));
            
            // Transform operations should be sent as POST requests
            Assert.AreEqual(HttpMethod.Post, Helper.LastMethod);
            
            // Verify that the last request body was sent as JSON
            Assert.AreEqual(MediaTypeNames.Application.Json, Helper.LastRequest!.Content!.Headers!.ContentType!.MediaType);
            
            // Verify that the request matches the expected JSON Schema
            var schema = await JsonSchema.FromJsonAsync(Helper.AwsStoreSchema);
            Assert.AreEqual(0, schema.Validate(Helper.LastBody).Count);
            
            // Parse the schema to be sure it contains the proper service and SecretKey data
            using var document = JsonDocument.Parse(Helper.LastBody);
            var store = document.RootElement.GetProperty("store");
            var service = store.GetProperty("service");
            Assert.AreEqual("s3", service.GetString());
            var secretKey = store.GetProperty("aws_secret_access_key");
            Assert.AreEqual("MY_SECRET_ACCESS_KEY", secretKey.GetString());
        }
        
        [Test]
        public async Task OptimizedImage_TransformStoreGCS_FormatsRequest()
        {
            var gcsOperation = new GoogleCloudStoreOperation()
            {
                GcpAccessToken = "MY_GCS_ACCESS_TOKEN",
                Path = "my-bucket/my-images/stored.image.jpg",
                Headers = new CloudStoreHeaders("public, max-age=31536000"),
            };
            await using var imageResult =
                await _optimizedImage.TransformImage(new TransformOperations(gcsOperation));
            
            // Transform operations should be sent as POST requests
            Assert.AreEqual(HttpMethod.Post, Helper.LastMethod);
            
            // Verify that the last request body was sent as JSON
            Assert.AreEqual(MediaTypeNames.Application.Json, Helper.LastRequest!.Content!.Headers!.ContentType!.MediaType);
            
            // Parse the schema to be sure it contains the proper service and SecretKey data
            using var document = JsonDocument.Parse(Helper.LastBody);
            var store = document.RootElement.GetProperty("store");
            var service = store.GetProperty("service");
            Assert.AreEqual("gcs", service.GetString());
            var secretKey = store.GetProperty("gcp_access_token");
            Assert.AreEqual("MY_GCS_ACCESS_TOKEN", secretKey.GetString());
            
            // Verify that the request matches the expected JSON Schema
            var schema = await JsonSchema.FromJsonAsync(Helper.GooglsCloudStoreSchema);
            Assert.AreEqual(0, schema.Validate(Helper.LastBody).Count);
        }
    }
}