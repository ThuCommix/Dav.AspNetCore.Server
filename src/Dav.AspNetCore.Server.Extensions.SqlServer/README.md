# SqlServer
## Installation

Install Dav.AspNetCore.Server.Extensions.SqlServer via dotnet cli or through the package manager provided by your favorite IDE.

```cmd
> dotnet add package Dav.AspNetCore.Server.Extensions.SqlServer
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

    davBuilder.AddSqlLocks(options => options.ConnectionString = "Server=127.0.0.1;Port=5432;Database=webdav;User Id=root;Password=root;");
    davBuilder.AddSqlPropertyStore(options =>
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
    Id varchar(max) NOT NULL,
    Uri varchar(max) NOT NULL,
    LockType INTEGER NOT NULL,
    Owner varchar(max) NOT NULL,
    Recursive bit NOT NULL,
    Timeout BIGINT NOT NULL,
    Issued BIGINT NOT NULL,
    Depth INTEGER NOT NULL
);
```

```sql
CREATE TABLE dav_aspnetcore_server_property
(
  	Uri varchar(max) NOT NULL,
  	ElementName varchar(max) NOT NULL,
  	ElementNamespace varchar(max) NOT NULL,
  	ElementValue varchar(max) NOT NULL
);
```