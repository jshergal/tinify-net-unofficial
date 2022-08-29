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
using TinifyAPI;

class Compress
{
  static void Main()
  {
    Tinify.Key = "YOUR_API_KEY";
    Tinify.FromFile("unoptimized.png").ToFile("optimized.png").Wait();
  }
}
```

## Running tests

```
dotnet restore
dotnet test test/Tinify.Tests
```

### Integration tests

```
dotnet restore
TINIFY_KEY=$YOUR_API_KEY dotnet test test/Tinify.Tests.Integration
```
Or add a `.env` file to the `/test/Tinify.Tests.Integration` directory in the format
```
TINIFY_KEY=<YOUR_API_KEY>
```

## License

This software is licensed under the MIT License. [View the license](LICENSE).
