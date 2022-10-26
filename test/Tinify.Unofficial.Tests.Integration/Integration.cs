using MetadataExtractor;
using MetadataExtractor.Formats.FileType;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Formats.WebP;
using MetadataExtractor.Formats.Xmp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tinify.Unofficial;
using MetadataDirectory = MetadataExtractor.Directory;

// ReSharper disable InconsistentNaming

namespace Tinify.Unofficial.Tests.Integration
{
    internal sealed class ImageMetadata
    {
        private const string ImagePngMimeTypeString = "image/png";
        private const string ImageJpegMimeTypeString = "image/jpeg";
        private const string ImageWebPMimeTypeString = "image/webp";

        private readonly IReadOnlyList<MetadataDirectory> _metaDataDirectories;

        private string _imageFileType;
        public string ImageFileType => _imageFileType ??= GetImageFileType();

        public ImageMetadata(string fileName)
        {
            _metaDataDirectories = ImageMetadataReader.ReadMetadata(fileName);
        }

        public bool IsPng => ImageFileType.Equals(ImagePngMimeTypeString, StringComparison.Ordinal);
        public bool IsJpeg => ImageFileType.Equals(ImageJpegMimeTypeString, StringComparison.Ordinal);
        public bool IsWebP => ImageFileType.Equals(ImageWebPMimeTypeString, StringComparison.Ordinal);

        private string GetImageFileType()
        {
            var fileTypeDir = _metaDataDirectories.OfType<FileTypeDirectory>().FirstOrDefault();
            if (fileTypeDir is null) return "unknown";
            return fileTypeDir.GetObject(FileTypeDirectory.TagDetectedFileMimeType) as string;
        }

        public int GetImageWidth()
        {
            return ImageFileType switch
            {
                ImagePngMimeTypeString => GetWidthFromPng(),
                ImageJpegMimeTypeString => GetWidthFromJpeg(),
                ImageWebPMimeTypeString => GetWidthFromWebP(),
                _ => -1
            };
        }

        public bool ContainsStringInXmpData(string toFind)
        {
            var dir = _metaDataDirectories.FirstOrDefault(d => d.Name.Equals("XMP", StringComparison.Ordinal));
            return (dir is XmpDirectory xmp) &&
                   xmp.GetXmpProperties().Any(pair => pair.Value.Equals(toFind, StringComparison.Ordinal));
        }

        private int GetWidthFromPng()
        {
            var header = _metaDataDirectories.FirstOrDefault(d => d.Name.Equals("PNG-IHDR", StringComparison.Ordinal));
            return (header?.GetObject(PngDirectory.TagImageWidth) as int?) ?? -1;
        }

        private int GetWidthFromJpeg()
        {
            var header = _metaDataDirectories.FirstOrDefault(d => d.Name.Equals("JPEG", StringComparison.Ordinal));
            var tag = header?.GetObject(JpegDirectory.TagImageWidth);
            if (tag is null) return -1;

            // Jpeg exif width tag is a Uint16, so need to use convert
            return Convert.ToInt32(tag);
        }

        private int GetWidthFromWebP()
        {
            var header = _metaDataDirectories.FirstOrDefault(d => d.Name.Equals("WebP", StringComparison.Ordinal));
            return (header?.GetObject(WebPDirectory.TagImageWidth) as int?) ?? -1;
        }
    }

    [TestFixture]
    public class Client_Integration
    {
        private static OptimizedImage optimized;

        private const string VoormediaCopyright = "Copyright Voormedia";

        private static TinifyClient _client;

        private static string _awsAccessId;
        private static string _awsSecretKey;
        private static string _awsRegion;
        private static string _awsBucket;

        [OneTimeSetUp]
        public static void Init()
        {
            DotNetEnv.Env.Load();

            var key = Environment.GetEnvironmentVariable("TINIFY_KEY");
            _client = new TinifyClient(key);

            _awsAccessId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            _awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            _awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
            _awsBucket = Environment.GetEnvironmentVariable("AWS_BUCKET");

            var unoptimizedPath = Path.Combine(AppContext.BaseDirectory, "examples", "voormedia.png");
            optimized = _client.ShrinkFromFile(unoptimizedPath).Result;
        }

        [Test]
        public async Task Should_Compress_FromFile()
        {
            await using var file = new TempFile();
            await optimized.ToFileAsync(file.Path).ConfigureAwait(false);

            var size = new FileInfo(file.Path).Length;
            Assert.Greater(size, 1000);
            Assert.Less(size, 1500);

            var metaData = new ImageMetadata(file.Path);
            Assert.That(metaData.IsPng);

            /* width == 137 */
            Assert.AreEqual(137, metaData.GetImageWidth());
            Assert.IsFalse(metaData.ContainsStringInXmpData(VoormediaCopyright));
        }

        [Test]
        public async Task Should_Compress_FromUrl()
        {
            await using var source = await _client.ShrinkFromUrl(
                "https://raw.githubusercontent.com/tinify/tinify-python/master/test/examples/voormedia.png"
            ).ConfigureAwait(false);

            await using var file = new TempFile();
            await source.ToFileAsync(file.Path).ConfigureAwait(false);

            var size = new FileInfo(file.Path).Length;
            Assert.Greater(size, 1000);
            Assert.Less(size, 1500);

            var metaData = new ImageMetadata(file.Path);
            Assert.That(metaData.IsPng);

            /* width == 137 */
            Assert.AreEqual(137, metaData.GetImageWidth());
            Assert.IsFalse(metaData.ContainsStringInXmpData(VoormediaCopyright));
        }

        [Test]
        public async Task Should_Resize()
        {
            await using var result = await optimized.TransformImage(new TransformOperations(
                new ResizeOperation(ResizeType.Fit, 50, 20)));

            await using var file = new TempFile();
            await result.ToFileAsync(file.Path).ConfigureAwait(false);
            var size = new FileInfo(file.Path).Length;
            Assert.Greater(size, 500);
            Assert.Less(size, 1000);

            var metaData = new ImageMetadata(file.Path);
            Assert.That(metaData.IsPng);

            /* width == 50 */
            Assert.AreEqual(50, metaData.GetImageWidth());
            Assert.IsFalse(metaData.ContainsStringInXmpData(VoormediaCopyright));
        }

        [Test]
        public async Task Should_PreserveMetadata()
        {
            await using var file = new TempFile();
            await using var result = await optimized.TransformImage(
                    new TransformOperations(
                        new PreserveOperation(PreserveOptions.Copyright | PreserveOptions.Creation)))
                .ConfigureAwait(false);
            await result.ToFileAsync(file.Path).ConfigureAwait(false);

            var size = new FileInfo(file.Path).Length;
            Assert.Greater(size, 1000);
            Assert.Less(size, 2000);

            var metaData = new ImageMetadata(file.Path);
            Assert.That(metaData.IsPng);

            /* width == 137 */
            Assert.AreEqual(137, metaData.GetImageWidth());
            Assert.IsTrue(metaData.ContainsStringInXmpData(VoormediaCopyright));
        }

        [Test]
        public async Task Should_Resize_And_PreserveMetadata()
        {
            await using var file = new TempFile();
            var resizeOptions = new ResizeOperation(ResizeType.Fit, 50, 20);
            var preserveOptions = new PreserveOperation(PreserveOptions.Copyright | PreserveOptions.Creation);
            await using var result = await optimized
                .TransformImage(new TransformOperations(resizeOptions, preserveOptions)).ConfigureAwait(false);
            await result.ToFileAsync(file.Path);

            var size = new FileInfo(file.Path).Length;
            Assert.Greater(size, 500);
            Assert.Less(size, 1100);

            var metaData = new ImageMetadata(file.Path);
            Assert.That(metaData.IsPng);

            /* width == 50 */
            Assert.AreEqual(50, metaData.GetImageWidth());
            Assert.IsTrue(metaData.ContainsStringInXmpData(VoormediaCopyright));
        }

        [Test]
        public async Task Should_Store_Aws()
        {
            if (string.IsNullOrWhiteSpace(_awsAccessId) || string.IsNullOrWhiteSpace(_awsSecretKey) ||
                string.IsNullOrWhiteSpace(_awsRegion) || string.IsNullOrWhiteSpace(_awsBucket)) return;

            var dest = _awsBucket + "/my-images/voormedia.optimized.png";
            await using var result = await optimized.TransformImage(new TransformOperations(
                new AwsCloudStoreOperation()
                {
                    Region = _awsRegion,
                    AwsAccessKeyId = _awsAccessId,
                    AwsSecretAccessKey = _awsSecretKey,
                    Path = dest,
                }));

            Assert.AreEqual($"/{dest}", result.Location?.LocalPath);
        }
    }
}