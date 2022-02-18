## Basic Usage

```
var options = DbClientOptions.New(
    connectionString: "Server=(localdb)\\MSSQLLocalDb;Initial Catalog=master;",
    providerFactory: SqlClientFactory.Instance // Depends on Microsoft.Data.SqlClient package.
);
var client = DbClient.New(options: options);

_ = client
    .ToIDbClient()
    .ExecuteStoredProcedure(
        name: "sp_tables",
        schemaName: "dbo"
    );
```

## ASP.NET Core (Version 6) Integration
**appsettings.json** (or equivalent)
```
{
    "ConnectionStrings": {
        "LocalDb": {
            "type": "Microsoft.Data.SqlClient.SqlClientFactory, Microsoft.Data.SqlClient, Version=4.1.0.0, Culture=neutral, PublicKeyToken=23ec7fc2d6eaa4a5",
            "value": "Server=(localdb)\\MSSQLLocalDb;Initial Catalog=master;"
        }
    },
    "Logging": {
        "LogLevel": {
            "ByteTerrace.Ouroboros.Database": "None" // Set to Trace for debugging output.
        }
    }
}
```

**Program.cs**
```
using ByteTerrace.Ouroboros.Database;

var builder = WebApplication.CreateBuilder(arges: args);

builder
    .Services
    .AddDbClients(configuration: builder.Configuration);
```

**ValuesController.cs**
```
using ByteTerrace.Ouroboros.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyNamespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExampleController : ControllerBase
    {
        public IDbClientFactory DbClientFactory { get; }

        public ExampleController(IDbClientFactory dbClientFactory) {
            DbClientFactory = dbClientFactory;
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("test-db-connection")]
        public async ValueTask<IActionResult> TestDbConnection(CancellationToken cancellationToken) {
            using var client = DbClientFactory.NewClient(name: "MyConnection");

            await client.OpenConnectionAsync(cancellationToken: cancellationToken);

            return Ok();
        }
    }
}
```

# Links
**NuGet References**
- [Lock File](https://devblogs.microsoft.com/nuget/enable-repeatable-package-restores-using-a-lock-file/)
- [Package ID Prefix Reservation](https://devblogs.microsoft.com/nuget/package-identity-and-trust/)
- [Source Link](https://devblogs.microsoft.com/dotnet/producing-packages-with-source-link/)
- [Source Mapping](https://devblogs.microsoft.com/nuget/introducing-package-source-mapping/)
