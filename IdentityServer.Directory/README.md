# AurorisNet.IdentityServer.Directory

Active Directory people and group lookup for .NET 10 (Windows only)—for address books, people pickers, and account lookups. Automatically installs `AurorisNet.ActiveDirectory`; does not require `AurorisNet.IdentityServer` or ASP.NET Core.

## Install

```shell
dotnet add package AurorisNet.IdentityServer.Directory
```

## Quick start

```csharp
using IdentityServer.Directory;

services.AddOptions<DirectoryOptions>().BindConfiguration(DirectoryOptions.SectionName);
services.AddSingleton<IDirectoryLookup, ActiveDirectoryLookup>();
```

```csharp
var people = await lookup.SearchUsersAsync("smith j", take: 10, scope: DirectoryScope.Global);
var user = await lookup.FindUserAsync(@"EXAMPLE\jsmith"); // by SID, GUID, or DOMAIN\user
var team = await lookup.ListGroupMembersAsync("Help Desk");
```

All LDAP filter inputs are escaped and queries run as the process account (application pool identity under IIS). Implement `IDirectoryUserFilter` to add custom LDAP filters or fetch extra attributes. Invalid requests throw `DirectoryQueryException`; unreachable directories throw `DirectoryUnavailableException`.

## Settings (`Directory` section)

| Setting | Default | Effect |
|---|---|---|
| `UserRoot` / `UserRootGlobal` / `GroupRoot` | whole domain | LDAP search roots for local user, global user/lookup, and group queries. |
| `GlobalSearchMinLength` | `2` | Minimum term length for global-scope searches. |
| `GlobalSearchSizeLimitBuffer` | `20` | Extra entries fetched before post-filtering. |
| `DefaultTake` / `MaxTake` | `20` / `50` | Default and maximum user search result count. |
| `MaxGroupMembers` | `500` | Maximum members returned per group lookup. |

See [Using the people lookup](https://github.com/auroris/AurorisNet.IdentityServer/blob/main/docs/embedding.md#using-the-people-lookup-alone) and [MIT License](https://github.com/auroris/AurorisNet.IdentityServer/blob/main/LICENSE).
