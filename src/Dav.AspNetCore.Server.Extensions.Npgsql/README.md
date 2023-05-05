# Npgsql
## Installation

Install Dav.AspNetCore.Server.Extensions.Npgsql via dotnet cli or through the package manager provided by your favorite IDE.

```cmd
> dotnet add package Dav.AspNetCore.Server.Extensions.Npgsql
```

## Getting started
This packages provides both: a property store, as well as a lock manager implementation. Based on what you want add either or both service registrations:

```csharp
builder.Services.AddWebDav(davBuilder =>
{
    davBuilder.AddLocalFiles(options =>
    {
        options.RootPath = "/tmp/";
    });

    davBuilder.AddNpgsqlLocks(options => options.ConnectionString = "Server=127.0.0.1;Port=5432;Database=webdav;User Id=root;Password=root;");
    davBuilder.AddNpgsqlPropertyStore(options =>
    {
        // This would also allow not defined properties to be accepted
        options.AcceptCustomProperties = true;
        options.ConnectionString = "Server=127.0.0.1;Port=5432;Database=webdav;User Id=root;Password=root;";
    });
});
```

## Schema
The schema **needs to be created beforehand**, there is no migration on app start.
```sql
CREATE TABLE dav_aspnetcore_server_resource_lock 
(
	Id text NOT NULL,
  	Uri text NOT NULL,
  	LockType INTEGER NOT NULL,
  	Owner text NOT NULL,
  	Recursive boolean NOT NULL,
  	Timeout INTEGER NOT NULL,
  	Issued INTEGER NOT NULL,
  	Depth INTEGER NOT NULL
);
```

```sql
CREATE TABLE dav_aspnetcore_server_property
(
  	Uri text NOT NULL,
  	ElementName text NOT NULL,
  	ElementNamespace text NOT NULL,
  	ElementValue text NOT NULL
);
```