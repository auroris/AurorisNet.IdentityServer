# AurorisNet.ActiveDirectory

Windows-only .NET 10 wrappers over `System.DirectoryServices` (`ADUser`, `ADGroup`, `ADComputer`, `ADPrinter`, `DirectorySearch`) in the `ActiveDirectory` namespace. Installed automatically by `AurorisNet.IdentityServer` and `AurorisNet.IdentityServer.Directory`.

## Install

```shell
dotnet add package AurorisNet.ActiveDirectory
```

## Quick start

```csharp
using ActiveDirectory;

using var user = new ADUser(@"EXAMPLE\alice"); // SAM name, DOMAIN\user, or LDAP:// path
Console.WriteLine($"{user.DisplayName} <{user.Email}>");

var search = new DirectorySearch("LDAP://OU=Staff,DC=example,DC=com", ADObject.ObjectClass.User);
search.PropertiesToSearch.Add(ADProperties.User.Surname, "Smith", DirectorySearch.MatchType.StartsWith);
search.PropertiesToLoad.Add(ADProperties.User.DisplayName);
foreach (ADUser found in search.Find<ADUser>(25))
{
    using (found) Console.WriteLine(found.DisplayName);
}
```

Dispose wrappers after use. `PropertiesToSearch` escapes values; `AdditionalFilter` is raw LDAP (trusted inputs only).

See [Using the Active Directory library](https://github.com/auroris/AurorisNet.IdentityServer/blob/main/docs/embedding.md#using-the-active-directory-library-alone) and [MIT License](https://github.com/auroris/AurorisNet.IdentityServer/blob/main/LICENSE).
