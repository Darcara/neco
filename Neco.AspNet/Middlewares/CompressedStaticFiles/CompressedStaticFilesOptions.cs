namespace Neco.AspNet.Middlewares.CompressedStaticFiles;

using System;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Neco.Common.Data;

public class CompressedStaticFilesOptions {
	/// <summary>
	/// <para>Use for SPA to serve 'index.html' or similar if requested file does not exist.</para>
	/// <para>Default is null which disables this behavior</para>
	/// </summary>
	public String? ServeOnNotFound {
		get;
		set {
			if (value == null || value.StartsWith('/')) field = value;
			else
				field = $"/{value}";
		}
	}

	/// <summary>
	/// The relative request path that maps to static resources.
	/// </summary>
	public String RequestPath { get; set; } = String.Empty;

	/// <summary>
	/// Used to map files to content-types.
	/// </summary>
	public IContentTypeProvider? ContentTypeProvider { get; set; }

	/// <summary>
	/// The default content type for a request if the ContentTypeProvider cannot determine one. None is provided by default, so the client must determine the format themselves
	/// </summary>
	public String? DefaultContentType { get; set; }

	/// <summary>
	/// If the file is not a recognized content-type by <see cref="ContentTypeProvider"/> should it be served? Default: false.
	/// </summary>
	public Boolean ServeUnknownFileTypes { get; set; }

	/// <summary>
	/// The file system used to locate resources
	/// </summary>
	public IFileProvider? FileProvider { get; set; }

	/// <summary>
	/// Lookup to determine if files should be compressed on response
	/// </summary>
	public IFileCompressionLookup? CompressionLookup { get; set; }
}