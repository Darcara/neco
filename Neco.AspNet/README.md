# NeCo - Necessary Code - AspNet

[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/neco.aspnet)](https://www.nuget.org/packages/Neco.AspNet/)
![GitHub License](https://img.shields.io/github/license/darcara/neco)

## CompressedStaticFiles

An ASP.NETCore middleware to serve static files. The files will be compressed on first request and saved to the cache, so they can be served immediately after.
If compressed files are already available, they will be used.

Huge folders of incompressible files are better served by StaticFilesMiddleware.  
Inspired by [CompressedStaticFiles by AnderssonPeter](https://github.com/AnderssonPeter/CompressedStaticFiles)

Usage:
```c#

```

### RelaxedPhysicalFileProvider 
This is a [PhysicalFileProvider](https://learn.microsoft.com/dotnet/api/microsoft.extensions.fileproviders.physicalfileprovider?view=dotnet-plat-ext-6.0) that does not require the path to exist during startup. Calls to [Watch](https://learn.microsoft.com/dotnet/api/microsoft.extensions.fileproviders.physicalfileprovider.watch?view=dotnet-plat-ext-6.0) will throw and all requested files will return [NotFound](https://learn.microsoft.com/dotnet/api/microsoft.extensions.fileproviders.notfoundfileinfo?view=dotnet-plat-ext-6.0).  

ToDo:

* [ ] Serve incompressible and NONE-compression files directly
* [x] Optionally serve uncompressed file while compressing
* [ ] Custom compression cache folder or CacheProvider
* [ ] GZip Compression
* [ ] Custom response / cache headers
* [ ] Ranges
