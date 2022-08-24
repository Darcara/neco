// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Neco.AspNet.Middlewares.CompressedStaticFiles;

public static class ApplicationBuilderExtensions {
	public static IApplicationBuilder UseCompressedStaticFiles(this IApplicationBuilder app, CompressedStaticFilesOptions options) {
		if (app == null)
			throw new ArgumentNullException(nameof(app));

		return app.UseMiddleware<CompressedStaticFilesMiddleware>(Options.Create(options));
	}

	public static IApplicationBuilder UseCompressedStaticFiles(this IApplicationBuilder app) => UseCompressedStaticFiles(app, new CompressedStaticFilesOptions());
}