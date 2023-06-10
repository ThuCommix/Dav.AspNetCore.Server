[![CI](https://github.com/ThuCommix/Dav.AspNetCore.Server/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/ThuCommix/Dav.AspNetCore.Server/actions/workflows/ci.yml)
[![MIT License](https://img.shields.io/static/v1?label=License&message=MIT&color=success)](https://github.com/ThuCommix/Dav.AspNetCore.Server/blob/main/LICENSE)
[![Nuget](https://img.shields.io/nuget/v/Dav.AspNetCore.Server)](https://www.nuget.org/packages/Dav.AspNetCore.Server/)

# WebDAV for ASP.NET Core

Dav.AspNetCore.Server is a WebDAV implementation based on <a href="http://www.webdav.org/specs/rfc4918.html">RFC 4918</a>.
It allows you to easily integrate DAV functionality into your ASP.NET Core application. Some architectural concepts where taken from <a href="https://github.com/ramondeklein/nwebdav">NWebDav</a> but where greatly improved upon.

## Features
- RFC 4918 compliant
- Supports any registered authentication, but also ships with Basic and Digest authentication
- Extensible infrastructure which lets you design your own store or locking providers

## Installation

Install Dav.AspNetCore.Server via dotnet cli or through the package manager provided by your favorite IDE.

```cmd
> dotnet add package Dav.AspNetCore.Server
```
## Getting started

In order to enable WebDAV in your project you need to add the following service registrations and middlewares:

```csharp
using Dav.AspNetCore.Server;
using Dav.AspNetCore.Server.Store;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebDav(davBuilder =>
{
    // add the local files store with a mount point
    davBuilder.AddLocalFiles(options =>
    {
        options.RootPath = "/tmp/";
    });
});

var app = builder.Build();

app.Map("/dav", davApp =>
{
    davApp.UseWebDav();
});

app.Run();
```

## Add locking support

There are different types of locking implementations available. 
If you need something simple you can start out with the in memory lock implementation:

```csharp
builder.Services.AddWebDav(davBuilder =>
{
    [...]
    davBuilder.AddInMemoryLocks();
});
```

In case you need something more distributed you can check out the other sql based implementations:
- [Sqlite](src/Dav.AspNetCore.Server.Extensions.Sqlite/README.md)
- [SqlServer](src/Dav.AspNetCore.Server.Extensions.SqlServer/README.md)
- [PostgreSQL](src/Dav.AspNetCore.Server.Extensions.Npgsql/README.md)

## Accepting properties

Storing (custom) properties is a crucial part of DAV. To start accepting properties you need to configure
a property store. Like previously mentioned in the locking section there are different implementations available:

```csharp
builder.Services.AddWebDav(davBuilder =>
{
    [...]
    
    // there will be a xml file containing properties for each resource made available
    // it's important to not expose this folder
    
    davBuilder.AddXmlFilePropertyStore(options =>
    {
        options.AcceptCustomProperties = true;
        options.RootPath = "/tmp_meta/";
    });
});
```

You may ask: what exactly is a "custom" property; A custom property is a property not made available by the
dav resource itself, it can be arbitrary data. Since this example uses the local file store, all properties
are computed and thus can't be changed which only leaves us with adding additional properties. On different
dav resources with normal properties (not protected and not calculated) you can change them without having
`AcceptCustomProperties = true`.

Different sql based implementations are available here:
- [Sqlite](src/Dav.AspNetCore.Server.Extensions.Sqlite/README.md)
- [SqlServer](src/Dav.AspNetCore.Server.Extensions.SqlServer/README.md)
- [PostgreSQL](src/Dav.AspNetCore.Server.Extensions.Npgsql/README.md)

## Contributing
Feel free to open issues or submit pullrequests.