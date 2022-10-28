![GitHub Workflow Status](https://img.shields.io/github/workflow/status/jshergal/tinify-net-unofficial/CI_CD)
![GitHub last commit](https://img.shields.io/github/last-commit/jshergal/tinify-net-unofficial)
![Nuget](https://img.shields.io/nuget/v/tinify.unofficial)

## Unofficial fork of the Tinify API Client for .NET

[Official Tinify GitHub repo](https://github.com/tinify/tinify-net)

.NET client for the Tinify API, used for [TinyPNG](https://tinypng.com) and [TinyJPG](https://tinyjpg.com). Tinify
compresses your images intelligently. Read more at [http://tinify.com](http://tinify.com).

## Installation

Install the API client:

```
Install-Package Tinify.Unofficial
```

### :boom: Minor Breaking Change between v1.0.2 and v1.0.3
The `Shrink` methods on the `TinifyClient` were renamed:
* `ShrinkFromFile`&emsp;&ensp;=>&nbsp;`ShrinkFromFileAsync`
* `ShrinkFromBuffer`&ensp;=>&nbsp;`ShrinkFromBufferAsync`
* `ShrinkFromUrl`&emsp;&emsp;=>&nbsp;`ShrinkFromUrlAsync`

## Usage

### Initialize the TinifyClient

Simply construct a new instance of `TinifyClient` with your [API key](https://tinypng.com/developers)

#### Example

```csharp
using Tinify.Unofficial;
var client = new TinifyClient("YOUR_API_KEY");
```

The client constructor also allows for the specifying of
an [HttpMessageHandler](https://learn.microsoft.com/en-us/dotnet/api/System.Net.Http.HttpMessageHandler)
which allows you to manage any kind of custom or specialized handling needed: specifying a proxy, special SSL handling,
etc. The `TinifyClient`
does not assume ownership of this handler, you are responsible for managing its lifetime.   

If no handler is specified then the default [SocketsHttpHandler](https://learn.microsoft.com/en-us/dotnet/api/System.Net.Http.SocketsHttpHandler)
with a `PooledConnectionLifetime` setting of 5 minutes is used. (Note: this also applies to the .netstandard 2.1 target
see [StandardSocketsHttpHandler](https://github.com/TalAloni/StandardSocketsHttpHandler))

#### Example

```csharp
public class MyProxy : IWebProxy
{
    private readonly Uri _uri;
    public ICredentials Credentials { get; set; }

    public MyProxy(string url)
    {
        Uri.TryCreate(url, UriKind.Absolute, out _uri));
        var user = _uri.UserInfo.Split(':');
        Credentials = new NetworkCredential(user[0], user[1]);
    }

    public Uri GetProxy(Uri destination) => _uri;

    public bool IsBypassed(Uri host) => host.IsLoopback;
}
```
___
```csharp
var client = new TinifyClient("YOUR_API_KEY",
    new HttpClientHandler()
    {
        Proxy = new MyProxy(@"http://user:pass@localhost:8080"),
        UserProxy = true,
    });
```

### Compress image using one of the `ShrinkFrom` methods

* `ShrinkFromFileAsync`
* `ShrinkFromBufferAsync`
* `ShrinkFromUrlAsync`
* `ShrinkFromStreamAsync`

#### Example

```csharp
var client = new TinifyClient("YOUR_API_KEY");
await using var optimizedImage = await client.ShrinkFromFileAsync("unoptimized.png");
await optimizedImage.ToFileAsync("optimized.png");
```

This will upload the image to the Tinify image service and will return an `OptimizedImage`

### Check current compression count

You can check your current [compression count](https://tinyjpg.com/developers/reference#compression-count)
by accessing the static `CompressionCount` property of the `TinifyClient` class. This count is updated each
time a response from the Tinify endpoint returns a compression count.

#### Example

```csharp
var currentCompressionCount = TinifyClient.CompressionCount;
```

### Other Tinify Transform Operations

To perform other transform operations (resize, convert, etc) simply call the `TransformImage` method
on the optimized image. The `TransformImage` operation takes a single `TransformOperations` argument.
The `TransformOperations` object must be constructed with at least one operation. Each of the Tinify
operations are by the different operations that can be specified in the `TransformOperations` constructor.

| Tinify API Operation                                                     | Tinify.Unofficial.Operation |
|:-------------------------------------------------------------------------|:----------------------------|
| [resize](https://tinyjpg.com/developers/reference#resizing-images)       | `ResizeOperation`           |
| [convert](https://tinyjpg.com/developers/reference#converting-images)    | `ConvertOperation`          |
| [preserve](https://tinyjpg.com/developers/reference#preserving-metadata) | `PreserveOperation`         |
| [store](https://tinyjpg.com/developers/reference#saving-to-amazon-s3)    | `CloudStoreOperation`*      |  

**note:* `CloudStoreOperation` is an abstract class and has concrete implementations
as `AwsCloudStoreOperation` and `GoogleCloudStoreOperation`

#### Example

```csharp
await using var optimizedImage = await client.ShrinkFromFile("unoptimized.jpg");

var resizeOptions = new ResizeOperation(ResizeType.Fit, 50, 20);
var preserveOptions = new PreserveOperation(PreserveOptions.Copyright | PreserveOptions.Creation);
var transformOperations = new TransformOperations(resize: resizeOptions, preserve: preserveOptions);
await using var result = await optimizedImage.TransformImage(transformOperations);

await result.ToFileAsync("optimized_and_transformed.jpg");
```

__*Note:*__  
Because both `OptimizedImage` and `ImageResult` objects maintain an internal buffer
of the image data, which has been rented from
the [ArrayPool](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1),
you should be sure to `Dispose` of them so that the buffer is returned to the pool.
Both objects implement `IDisposable` and `IAsyncDisposable` so that they
can be easily wrapped in using blocks or statements.

### Saving the optimized or transformed image

You can save both `OptimizedImage` and `ImageResult` objects to a file, to a stream, to a buffer or pass in a
preallocated buffer and copy the data directly to the buffer

#### Example

```csharp
await using var optimizedImage = await client.ShrinkFromFile("unoptimized.jpg");
await using var transformedImage =
    await optimizedImage.TransformImage(new TransformOperations(
        resize: new ResizeOperation(ResizeType.Fit, 50, 20)
    ));
                                    
var optimizedBuffer = await optimizedImage.ToBufferAsync();

// Note the ImageResult object already holds an internal buffer
// with the image data and so will just return a copy synchronously
var transformedBuffer = transformedImage.ToBuffer();

using var msOptimized = new MemoryStream();
await optimizedImage.ToStreamAsync(msOptimized);

using var msTransformed = new MemoryStream();
await transformedImage.ToStreamAsync(msTransformed);

var bufferOptimized = new byte[optimizedImage.ImageSize.Value];
await optimizedImage.CopyToBufferAsync(bufferOptimized);

// Note the ImageResult object already holds an internal buffer
// with the image data and so will just copy the data synchronously
var bufferTransformed = new byte[transformedImage.DataLength];
transformedImage.CopyToBuffer(bufferTransformed);
```

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
