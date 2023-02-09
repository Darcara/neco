namespace Neco.AspNet;

using System;

public interface IFilesystemChangeNotifier {
	public void NotifyFileChanged(String relativePath);
	public event OnFileChangedDelegate OnFileChanged;
}

public delegate void OnFileChangedDelegate(Object sender, OnFileChangedArgs args);

public class OnFileChangedArgs {
	public readonly String RelativePath;

	public OnFileChangedArgs(String relativePath) {
		RelativePath = relativePath;
	}
}

public class FilesystemChangeNotifier : IFilesystemChangeNotifier {
	#region Implementation of IFilesystemChangeNotifier

	/// <inheritdoc />
	public void NotifyFileChanged(String relativePath) {
		OnFileChanged?.Invoke(this, new OnFileChangedArgs(relativePath));
	}

	/// <inheritdoc />
	public event OnFileChangedDelegate? OnFileChanged;

	#endregion
}