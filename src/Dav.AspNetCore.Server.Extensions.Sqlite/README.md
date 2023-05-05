# Sqlite
## Installation

Install Dav.AspNetCore.Server.Extensions.Sqlite via dotnet cli or through the package manager provided by your favorite IDE.

```cmd
> dotnet add package Dav.AspNetCore.Server.Extensions.Sqlite
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

    davBuilder.AddSqliteLocks(options => options.ConnectionString = "Data Source=/data/sqlite.db;");
    davBuilder.AddSqlitePropertyStore(options =>
    {
        // This would also allow not defined properties to be accepted
        options.AcceptCustomProperties = true;
        options.ConnectionString = "Data Source=/data/sqlite.db;";
    });
});
```

## Schema
The schema **needs to be created beforehand**, there is no migration on app start.
```sql
CREATE TABLE dav_aspnetcore_server_resource_lock 
(
	Id TEXT NOT NULL,
  	Uri TEXT NOT NULL,
  	LockType INTEGER NOT NULL,
  	Owner TEXT NOT NULL,
  	Recursive INTEGER NOT NULL,
  	Timeout INTEGER NOT NULL,
  	Issued INTEGER NOT NULL,
  	Depth INTEGER NOT NULL
);
```

```sql
CREATE TABLE dav_aspnetcore_server_property
(
  	Uri TEXT NOT NULL,
  	ElementName TEXT NOT NULL,
  	ElementNamespace TEXT NOT NULL,
  	ElementValue TEXT NOT NULL
);
```