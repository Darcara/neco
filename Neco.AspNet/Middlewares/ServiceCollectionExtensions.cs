// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

using System;
using Neco.AspNet;

public static class ServiceCollectionExtensions {
	public static void AddFilesystemCacheInvalidatior(this IServiceCollection services) {
		if (services == null) throw new ArgumentNullException(nameof(services));

		services.AddSingleton<IFilesystemChangeNotifier, FilesystemChangeNotifier>();

	}
}