# NeCo - Necessary Code - AspNet

[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/neco.aspnet)](https://www.nuget.org/packages/Neco.AspNet/)

## CompressedStaticFiles

An ASP.NETCore middleware to serve static files. The files will be compressed on first request and saved to the cache, so they can be served immediately after.
If compressed files are already available, they will be used.

Huge folders of incompressible files are better served by StaticFilesMiddleware.  
Inspired by [CompressedStaticFiles by AnderssonPeter](https://github.com/AnderssonPeter/CompressedStaticFiles)

Usage:
```c#

```

ToDo:

* [ ] Serve incompressible and NONE-compression files directly
* [ ] Optionally serve uncompressed file while compressing
* [ ] Custom compression cache folder or CacheProvider
* [ ] GZip Compression
* [ ] Custom response / cache headers
* [ ] Ranges
