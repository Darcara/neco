namespace Neco.AspNet.Middlewares.CompressedStaticFiles;

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

public class CompressedStaticFilesOptions {
	private String? _serveOnNotFound = null;

	/// <summary>
	/// <para>Use for SPA to serve 'index.html' or similar if requested file does not exist.</para>
	/// <para>Default is null which disables this behavior</para>
	/// </summary>
	public String? ServeOnNotFound {
		get => _serveOnNotFound;
		set {
			if (value == null || value.StartsWith('/')) _serveOnNotFound = value;
			else
				_serveOnNotFound = $"/{value}";
		}
	}

	/// <summary>
	/// Should configured endpoints be allowed to execute.
	/// </summary>
	/// This should only happen if this Middleware is between EndpointRouting- and Endpoint-middlewares
	/// <seealso cref="EndpointRoutingApplicationBuilderExtensions.UseEndpoints"/>
	public Boolean HonorEndpoints { get; set; } = false;

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
	public Boolean ServeUnknownFileTypes { get; set; } = false;

	/// <summary>
	/// The file system used to locate resources
	/// </summary>
	public IFileProvider? FileProvider { get; set; }
}