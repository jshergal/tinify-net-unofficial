using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tinify.Unofficial
{
	public record ImageTransformOperations
	{
		[JsonPropertyName("preserve")] public string[] Preserve { get; }

		[JsonPropertyName("resize")] public ResizeOperation Resize { get; }

		[JsonPropertyName("store")] public object CloudStore { get; }

		public static ImageTransformOperations CreatePreserveOperation(PreserveOptions options) =>
			new(new PreserveOperation(options));

		public static ImageTransformOperations CreateResizeOperation(ResizeType resizeType, int? width = null,
			int? height = null) => new(new ResizeOperation(resizeType, width, height));

		public static ImageTransformOperations CreateStoreOperation(AwsCloudStoreData cloudData) =>
			new(cloudData);

		public static ImageTransformOperations CreateStoreOperation(GoogleCloudStoreData cloudData) =>
			new(cloudData);

		private ImageTransformOperations(ResizeOperation resizeOperation) : this(resizeOperation, null, null)
		{
		}

		private ImageTransformOperations(PreserveOperation preserveOperation) : this(null, preserveOperation, null)
		{
		}

		private ImageTransformOperations(CloudStoreData cloudData) : this(null, null, cloudData)
		{
		}

		public ImageTransformOperations(ResizeOperation resizeOperation,
			PreserveOperation preserveOperation, CloudStoreData cloudData)
		{
			if (resizeOperation is null && preserveOperation is null && cloudData is null)
			{
				throw new ArgumentException("At least one transform operation must be specified");
			}

			Preserve = preserveOperation?.Options;
			Resize = resizeOperation;
			CloudStore = cloudData;
		}
	}

	[Flags]
	public enum PreserveOptions
	{
		None = 0,
		Copyright = 1 << 0,
		Creation = 1 << 1,
		Location = 1 << 2,
	}

	public sealed record PreserveOperation
	{
		internal string[] Options { get; }

		public PreserveOperation(PreserveOptions options)
		{
			var list = new List<string>(3);
			if (options.HasFlag(PreserveOptions.Copyright)) list.Add("copyright");
			if (options.HasFlag(PreserveOptions.Creation)) list.Add("creation");
			if (options.HasFlag(PreserveOptions.Location)) list.Add("location");
			Options = list.ToArray();
		}
	}

	public enum ResizeType
	{
		Scale,
		Fit,
		Cover,
		Thumb,
	}

	public sealed record ResizeOperation
	{
		[JsonPropertyName("method")] public string Method { get; }

		[JsonPropertyName("width")] public int? Width { get; }

		[JsonPropertyName("height")] public int? Height { get; }

		public ResizeOperation(ResizeType resizeType, int? width = null, int? height = null)
		{
			if (resizeType != ResizeType.Scale && (!width.HasValue || !height.HasValue))
			{
				throw new ArgumentException(
					$"Resize Type: '{resizeType:G}' requires both width and height to be specified");
			}

			if (resizeType == ResizeType.Scale && width.HasValue && height.HasValue)
			{
				throw new ArgumentException(
					$"You must specify either a width or a height but not both when using Resize Type: '{resizeType:G}'");
			}

			Method = ResizeTypeToString(resizeType);

			Width = width;
			Height = height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string ResizeTypeToString(ResizeType resizeType)
		{
			return resizeType switch
			{
				ResizeType.Cover => "cover",
				ResizeType.Fit => "fit",
				ResizeType.Scale => "scale",
				ResizeType.Thumb => "thumb",
				_ => throw new ArgumentException("Invalid enum value", nameof(resizeType)),
			};
		}
	}

	public abstract record CloudStoreData
	{
		internal string ToJsonData()
		{
			return JsonSerializer.Serialize<object>(this, TinifyConstants.SerializerOptions);
		}
	}
}