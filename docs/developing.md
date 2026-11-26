# Developing

Requires the .NET 10 SDK on Windows or Linux.

```powershell
dotnet build IdentityServer.sln --configuration Release
dotnet test IdentityServer.sln --configuration Release --filter 'Category!=LiveDirectory'
dotnet pack ActiveDirectory/ActiveDirectory.csproj --configuration Release --output artifacts/nuget
dotnet pack IdentityServer.Core/IdentityServer.Core.csproj --configuration Release --output artifacts/nuget
dotnet pack IdentityServer.Directory/IdentityServer.Directory.csproj --configuration Release --output artifacts/nuget
```

Run live directory tests locally on a domain-joined machine (or with LDAP env vars—see [verification.md](verification.md#live-directory-tests)):

```powershell
dotnet test IdentityServer.sln --configuration Release --filter 'Category=LiveDirectory'
```

## Solution layout

- `IdentityServer.csproj` — standalone host and sample `appsettings.json`
- `IdentityServer.Core` — `AurorisNet.IdentityServer` package
- `ActiveDirectory` — `AurorisNet.ActiveDirectory` package
- `IdentityServer.Directory` — `AurorisNet.IdentityServer.Directory` package
- `tests/` — server and directory lookup tests

## Releases

Publishes Windows/Linux archives, the three NuGet packages, and `SHA256SUMS.txt` (see [releasing.md](releasing.md) and the shared [release guide](https://github.com/auroris/AurorisNet.Build/blob/main/docs/releasing.md)).
