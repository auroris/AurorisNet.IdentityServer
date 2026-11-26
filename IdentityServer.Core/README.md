# AurorisNet.IdentityServer

Embeddable OpenID Connect server for ASP.NET Core 10 backed by Active Directory, Samba AD, or FreeIPA (built on [OpenIddict](https://documentation.openiddict.com/)). Runs on Windows and Linux.

Adds discovery, JWKS, authorization, and token endpoints using Negotiate, IIS Windows Authentication, or Kerberos reverse-proxy auth (no refresh tokens). Pulls in `AurorisNet.ActiveDirectory` automatically.

## Install

```shell
dotnet add package AurorisNet.IdentityServer
```

```csharp
// Program.cs
using IdentityServer;
IdentityServerHost.Run<Startup>(args);
```

```csharp
// Startup.cs
using IdentityServer;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddIdentityServer(configuration);
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseIdentityServer(env);
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
```

Configure the `IdentityServer` section in `appsettings.json` (`ServerUri`, `Hosts`, `Clients`, `Groups`, `PersistKeys`, `DataPath`, plus `Directory` on Linux), then verify `/.well-known/openid-configuration`.

## Documentation

- [Embedding guide](https://github.com/auroris/AurorisNet.IdentityServer/blob/main/docs/embedding.md)
- [Configuration reference](https://github.com/auroris/AurorisNet.IdentityServer/blob/main/docs/configuration.md)
- [Repository README](https://github.com/auroris/AurorisNet.IdentityServer)
- [MIT License](https://github.com/auroris/AurorisNet.IdentityServer/blob/main/LICENSE)
