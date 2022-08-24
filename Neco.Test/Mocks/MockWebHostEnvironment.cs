namespace Neco.Test.Mocks;

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

public class MockWebHostEnvironment : IWebHostEnvironment {
  public MockWebHostEnvironment(IFileProvider contentRootFileProvider) {
    ContentRootFileProvider = contentRootFileProvider;
  }

  #region Implementation of IHostEnvironment

  /// <inheritdoc />
  public String ApplicationName { get; set; } = "MockApp";

  /// <inheritdoc />
  public IFileProvider ContentRootFileProvider { get; set; }

  /// <inheritdoc />
  public String ContentRootPath { get; set; } = "/";

  /// <inheritdoc />
  public String EnvironmentName { get; set; } = "MockEnv";

  #endregion

  #region Implementation of IWebHostEnvironment

  /// <inheritdoc />
  public String WebRootPath {
    get => ContentRootPath;
    set => ContentRootPath = value;
  }

  /// <inheritdoc />
  public IFileProvider WebRootFileProvider {
    get => ContentRootFileProvider;
    set => ContentRootFileProvider = value;
  }

  #endregion
}