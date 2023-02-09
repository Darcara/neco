namespace Neco.AspNet;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Internal;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Primitives;
using Neco.Common.Helper;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <summary>
/// Looks up files using the on-disk file system
/// </summary>
/// <remarks>
/// When the environment variable "DOTNET_USE_POLLING_FILE_WATCHER" is set to "1" or "true", calls to
/// <see cref="Watch(string)" /> will use <see cref="PollingFileChangeToken" />.
/// </remarks>
public class RelaxedPhysicalFileProvider : IFileProvider, IDisposable {
	private const String _pollingEnvironmentKey = "DOTNET_USE_POLLING_FILE_WATCHER";

	private static readonly Char[] _pathSeparators = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

	private readonly ExclusionFilters _filters;

	private readonly Func<PhysicalFilesWatcher> _fileWatcherFactory;
	private PhysicalFilesWatcher? _fileWatcher;
	private Boolean _fileWatcherInitialized;
	private Object _fileWatcherLock = new();

	private Boolean? _usePollingFileWatcher;
	private Boolean? _useActivePolling;
	private Boolean _disposed;

	/// <summary>
	/// The root directory for this instance.
	/// </summary>
	public String Root { get; }

	/// <summary>
	/// Initializes a new instance of a PhysicalFileProvider at the given root directory.
	/// </summary>
	/// <param name="root">The root directory. This should be an absolute path.</param>
	/// <param name="filters">Specifies which files or directories are excluded.</param>
	public RelaxedPhysicalFileProvider(String root, ExclusionFilters filters = ExclusionFilters.Sensitive) {
		if (!Path.IsPathRooted(root)) {
			throw new ArgumentException("The path must be absolute.", nameof(root));
		}

		String fullRoot = Path.GetFullPath(root);
		// When we do matches in GetFullPath, we want to only match full directory names.
		Root = PathUtils.EnsureTrailingSlash(fullRoot);
		// if (!Directory.Exists(Root)) {
		// 	throw new DirectoryNotFoundException(Root);
		// }

		_filters = filters;
		_fileWatcherFactory = CreateFileWatcher;
	}

	/// <summary>
	/// Gets or sets a value that determines if this instance of <see cref="PhysicalFileProvider"/>
	/// uses polling to determine file changes.
	/// <para>
	/// By default, <see cref="PhysicalFileProvider"/>  uses <see cref="FileSystemWatcher"/> to listen to file change events
	/// for <see cref="Watch(string)"/>. <see cref="FileSystemWatcher"/> is ineffective in some scenarios such as mounted drives.
	/// Polling is required to effectively watch for file changes.
	/// </para>
	/// <seealso cref="UseActivePolling"/>.
	/// </summary>
	/// <value>
	/// The default value of this property is determined by the value of environment variable named <c>DOTNET_USE_POLLING_FILE_WATCHER</c>.
	/// When <c>true</c> or <c>1</c>, this property defaults to <c>true</c>; otherwise false.
	/// </value>
	public Boolean UsePollingFileWatcher {
		get {
			if (_fileWatcher != null) {
				return false;
			}

			if (_usePollingFileWatcher == null) {
				ReadPollingEnvironmentVariables();
			}

			return _usePollingFileWatcher ?? false;
		}
		set {
			if (_fileWatcher != null) {
				throw new InvalidOperationException("CannotModifyWhenFileWatcherInitialized: " + nameof(UsePollingFileWatcher));
			}

			_usePollingFileWatcher = value;
		}
	}

	/// <summary>
	/// Gets or sets a value that determines if this instance of <see cref="PhysicalFileProvider"/>
	/// actively polls for file changes.
	/// <para>
	/// When <see langword="true"/>, <see cref="IChangeToken"/> returned by <see cref="Watch(string)"/> will actively poll for file changes
	/// (<see cref="IChangeToken.ActiveChangeCallbacks"/> will be <see langword="true"/>) instead of being passive.
	/// </para>
	/// <para>
	/// This property is only effective when <see cref="UsePollingFileWatcher"/> is set.
	/// </para>
	/// </summary>
	/// <value>
	/// The default value of this property is determined by the value of environment variable named <c>DOTNET_USE_POLLING_FILE_WATCHER</c>.
	/// When <c>true</c> or <c>1</c>, this property defaults to <c>true</c>; otherwise false.
	/// </value>
	public Boolean UseActivePolling {
		get {
			if (_useActivePolling == null) {
				ReadPollingEnvironmentVariables();
			}

			return _useActivePolling.Value;
		}

		set => _useActivePolling = value;
	}

	private PhysicalFilesWatcher FileWatcher =>
		LazyInitializer.EnsureInitialized(
			ref _fileWatcher,
			ref _fileWatcherInitialized,
			ref _fileWatcherLock,
			_fileWatcherFactory)!;

	private PhysicalFilesWatcher CreateFileWatcher() {
		String root = PathUtils.EnsureTrailingSlash(Path.GetFullPath(Root));

		FileSystemWatcher? watcher;
#if NETCOREAPP
		//  For browser/iOS/tvOS we will proactively fallback to polling since FileSystemWatcher is not supported.
		if (OperatingSystem.IsBrowser() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()) || OperatingSystem.IsTvOS()) {
			UsePollingFileWatcher = true;
			UseActivePolling = true;
			watcher = null;
		} else
#endif
		{
			// When UsePollingFileWatcher & UseActivePolling are set, we won't use a FileSystemWatcher.
			watcher = UsePollingFileWatcher && UseActivePolling ? null : new FileSystemWatcher(root);
		}

		PhysicalFilesWatcher pfw = new(root, watcher, UsePollingFileWatcher, _filters);
		ReflectionHelper.SetFieldOrPropertyValue(pfw, nameof(UseActivePolling), false, () => true);
		return pfw;
	}

	[MemberNotNull(nameof(_usePollingFileWatcher))]
	[MemberNotNull(nameof(_useActivePolling))]
	private void ReadPollingEnvironmentVariables() {
		String? environmentValue = Environment.GetEnvironmentVariable(_pollingEnvironmentKey);
		Boolean pollForChanges = String.Equals(environmentValue, "1", StringComparison.Ordinal) ||
		                         String.Equals(environmentValue, "true", StringComparison.OrdinalIgnoreCase);

		_usePollingFileWatcher = pollForChanges;
		_useActivePolling = pollForChanges;
	}

	/// <summary>
	/// Disposes the provider. Change tokens may not trigger after the provider is disposed.
	/// </summary>
	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Disposes the provider.
	/// </summary>
	/// <param name="disposing"><c>true</c> is invoked from <see cref="IDisposable.Dispose"/>.</param>
	protected virtual void Dispose(Boolean disposing) {
		if (_disposed) return;
		if (disposing) {
			_fileWatcher?.Dispose();
		}

		_disposed = true;
	}

	private String? GetFullPath(String path) {
		if (PathUtils.PathNavigatesAboveRoot(path)) {
			return null;
		}

		String fullPath;
		try {
			fullPath = Path.GetFullPath(Path.Combine(Root, path));
		}
		catch {
			return null;
		}

		if (!IsUnderneathRoot(fullPath)) {
			return null;
		}

		return fullPath;
	}

	private Boolean IsUnderneathRoot(String fullPath) => fullPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Locate a file at the given path by directly mapping path segments to physical directories.
	/// </summary>
	/// <param name="subpath">A path under the root directory</param>
	/// <returns>The file information. Caller must check <see cref="Microsoft.Extensions.FileProviders.IFileInfo.Exists"/> property. </returns>
	public IFileInfo GetFileInfo(String subpath) {
		if (String.IsNullOrEmpty(subpath) || PathUtils.HasInvalidPathChars(subpath)) {
			return new NotFoundFileInfo(subpath);
		}

		// Relative paths starting with leading slashes are okay
		subpath = subpath.TrimStart(_pathSeparators);

		// Absolute paths not permitted.
		if (Path.IsPathRooted(subpath)) {
			return new NotFoundFileInfo(subpath);
		}

		String? fullPath = GetFullPath(subpath);
		if (fullPath == null) {
			return new NotFoundFileInfo(subpath);
		}

		FileInfo fileInfo = new(fullPath);
		if (IsExcluded(fileInfo, _filters)) {
			return new NotFoundFileInfo(subpath);
		}

		return new PhysicalFileInfo(fileInfo);
	}

	private static Boolean IsExcluded(FileInfo fileSystemInfo, ExclusionFilters filters) {
		if (filters == ExclusionFilters.None) {
			return false;
		}

		if (fileSystemInfo.Name.StartsWith(".", StringComparison.Ordinal) && (filters & ExclusionFilters.DotPrefixed) != 0) {
			return true;
		}

		return fileSystemInfo.Exists &&
		       (((fileSystemInfo.Attributes & FileAttributes.Hidden) != 0 && (filters & ExclusionFilters.Hidden) != 0) ||
		        ((fileSystemInfo.Attributes & FileAttributes.System) != 0 && (filters & ExclusionFilters.System) != 0));
	}

	/// <summary>
	/// Enumerate a directory at the given path, if any.
	/// </summary>
	/// <param name="subpath">A path under the root directory. Leading slashes are ignored.</param>
	/// <returns>
	/// Contents of the directory. Caller must check <see cref="Microsoft.Extensions.FileProviders.IDirectoryContents.Exists"/> property. <see cref="Microsoft.Extensions.FileProviders.NotFoundDirectoryContents" /> if
	/// <paramref name="subpath" /> is absolute, if the directory does not exist, or <paramref name="subpath" /> has invalid
	/// characters.
	/// </returns>
	public IDirectoryContents GetDirectoryContents(String subpath) {
		try {
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (subpath == null || PathUtils.HasInvalidPathChars(subpath)) {
				return NotFoundDirectoryContents.Singleton;
			}

			// Relative paths starting with leading slashes are okay
			subpath = subpath.TrimStart(_pathSeparators);

			// Absolute paths not permitted.
			if (Path.IsPathRooted(subpath)) {
				return NotFoundDirectoryContents.Singleton;
			}

			String? fullPath = GetFullPath(subpath);
			if (fullPath == null || !Directory.Exists(fullPath)) {
				return NotFoundDirectoryContents.Singleton;
			}

			return new PhysicalDirectoryContents(fullPath, _filters);
		}
		catch (DirectoryNotFoundException) {
		}
		catch (IOException) {
		}

		return NotFoundDirectoryContents.Singleton;
	}

	/// <summary>
	///     <para>Creates a <see cref="IChangeToken" /> for the specified <paramref name="filter" />.</para>
	///     <para>Globbing patterns are interpreted by <seealso cref="Matcher" />.</para>
	/// </summary>
	/// <param name="filter">
	/// Filter string used to determine what files or folders to monitor. Example: **/*.cs, *.*,
	/// subFolder/**/*.cshtml.
	/// </param>
	/// <returns>
	/// An <see cref="IChangeToken" /> that is notified when a file matching <paramref name="filter" /> is added,
	/// modified or deleted. Returns a <see cref="Microsoft.Extensions.FileProviders.NullChangeToken" /> if <paramref name="filter" /> has invalid filter
	/// characters or if <paramref name="filter" /> is an absolute path or outside the root directory specified in the
	/// constructor <seealso cref="PhysicalFileProvider(string)" />.
	/// </returns>
	public IChangeToken Watch(String filter) {
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (filter == null || PathUtils.HasInvalidFilterChars(filter)) {
			return NullChangeToken.Singleton;
		}

		// Relative paths starting with leading slashes are okay
		filter = filter.TrimStart(_pathSeparators);

		return FileWatcher.CreateFileChangeToken(filter);
	}
}