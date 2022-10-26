![GitHub Workflow Status](https://img.shields.io/github/workflow/status/jshergal/tinify-net-unofficial/CI_CD)
![GitHub last commit](https://img.shields.io/github/last-commit/jshergal/tinify-net-unofficial)
![Nuget](https://img.shields.io/nuget/v/tinify.unofficial)

## Unofficial fork of the Tinify API Client for .NET

[Official Tinify GitHub repo](https://github.com/tinify/tinify-net)

.NET client for the Tinify API, used for [TinyPNG](https://tinypng.com) and [TinyJPG](https://tinyjpg.com). Tinify compresses your images intelligently. Read more at [http://tinify.com](http://tinify.com).

## Installation

Install the API client:

```
Install-Package Tinify.Unofficial
```

## Usage

```csharp
using Tinify.Unofficial;

var client = new TinifyClient("YOUR_API_KEY");
await using var optimizedImage = await client.ShrinkFromFile("unoptimized.png");
await optimizedImage.ToFileAsync("optimized.png");
```

To perform other transform operations, simply call the `TransformImage` method
on the optimized image

```csharp
await using var optimizedImage = await client.ShrinkFromFile("unoptimized.jpg");

var resizeOptions = new ResizeOperation(ResizeType.Fit, 50, 20);
var preserveOptions = new PreserveOperation(PreserveOptions.Copyright | PreserveOptions.Creation);
var transformOperations = new TransformOperations(resizeOptions, preserveOptions);
await using var result = await optimized.TransformImage(transformOperations);

await result.ToFileAsync("optimized_and_transformed.jpg");
```

You can save both `OptimizedImage` and `Result` objects to a file, to a stream, to a buffer or pass in a preallocated buffer and copy the data directly to the buffer
```csharp
await using var optimizedImage = await client.ShrinkFromFile("unoptimized.jpg");
await using var transformedImage =
    await optimizedImage.TransformImage(new TransformOperations(
        new ResizeOperation(ResizeType.Fit, 50, 20)
    ));
                                    
var optimizedBuffer = await optimizedImage.ToBufferAsync();

// Note the Result object already holds an internal buffer
// with the image data and so will just return a copy synchronously
var transformedBuffer = transformedImage.ToBuffer();

using var msOptimized = new MemoryStream();
await optimizedImage.ToStreamAsync(msOptimized);

using var msTransformed = new MemoryStream();
await transformedImage.ToStreamAsync(msTransformed);

var bufferOptimized = new byte[optimizedImage.ImageSize];
await optimizedImage.CopyToBufferAsync(bufferOptimized);

// Note the Result object already holds an internal buffer
// with the image data and so will just copy the data synchronously
var bufferTransformed = new byte[transformedImage.DataLength];
transformedImage.CopyDataToBuffer(bufferTransformed);
```

__*Note:*__  
Because both `OptimizedImage` and `Result` objects maintain an internal buffer of the image data, you should be sure to call their `Dispose` methods or wrap them in a using block/statement.
Both objects implement both `IDisposable` and `IAsyncDisposable`

## Running tests

```
dotnet restore
dotnet test test/Tinify.Unofficial.Tests
```

### Integration tests

```
dotnet restore
TINIFY_KEY=$YOUR_API_KEY dotnet test test/Tinify.Unofficial.Tests.Integration
```
Or add a `.env` file to the `/test/Tinify.Unofficial.Tests.Integration` directory in the format
```
TINIFY_KEY=<YOUR_API_KEY>
```

## License

This software is licensed under the MIT License. [View the license](LICENSE).
