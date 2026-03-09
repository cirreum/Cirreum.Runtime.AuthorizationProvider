# Cirreum.Runtime.AuthorizationProvider

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Runtime.AuthorizationProvider.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.AuthorizationProvider/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Runtime.AuthorizationProvider.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.AuthorizationProvider/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Runtime.AuthorizationProvider?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Runtime.AuthorizationProvider/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Runtime.AuthorizationProvider?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Runtime.AuthorizationProvider/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Base registration infrastructure for Cirreum authorization providers**

## Overview

**Cirreum.Runtime.AuthorizationProvider** provides the runtime layer for registering authorization providers with ASP.NET Core applications. It includes the provider registration pattern, role enrichment via claims transformation, and diagnostics. This package is typically not referenced directly.

> **Note:** For the complete authorization system with all provider implementations (Entra ID, API Key, Signed Request, External/BYOID), see [Cirreum.Runtime.Authorization](https://github.com/cirreum/Cirreum.Runtime.Authorization). Most applications should reference that package instead of this one directly.

## Features

- **Type-safe provider registration** with generic constraints
- **Configuration-driven setup** using hierarchical configuration sections
- **Multiple provider instances** support per registrar type
- **Role enrichment** via `IRoleResolver` — resolves application roles from your data store and adds them as `ClaimTypes.Role` claims
- **Diagnostics** — `ActivitySource`, `Meter`, and per-request `ClaimsTransformResult` stashed in `HttpContext.Items` for debugging
- **Duplicate registration protection** with automatic detection
- **Structured logging** with deferred execution for performance
- **Seamless ASP.NET Core integration** through `IHostApplicationBuilder` extensions

## Installation

```bash
dotnet add package Cirreum.Runtime.AuthorizationProvider
```

## Usage

### Provider Registration

Authorization providers are registered during application startup. Each provider type has its own registrar that reads from the hierarchical configuration:

```csharp
var builder = DomainApplication.CreateBuilder(args);
var authBuilder = builder.Services.AddAuthentication();

// Register an authorization provider (typically called by the Runtime)
builder.RegisterAuthorizationProvider<EntraAuthorizationRegistrar, EntraAuthorizationSettings, EntraAuthorizationInstanceSettings>(authBuilder);
```

### Configuration Structure

Configure your authorization providers in `appsettings.json`. Instances are keyed by name:

```json
{
  "Cirreum": {
    "Authorization": {
      "Providers": {
        "Entra": {
          "Instances": {
            "Primary": {
              "Enabled": true,
              "Audience": "api://my-app",
              "TenantId": "your-tenant-id",
              "ClientId": "your-client-id"
            }
          }
        }
      }
    }
  }
}
```

### Role Enrichment

For audience-based providers (Entra, Okta, OIDC), you can resolve application roles from your data store and add them as claims. Register your `IRoleResolver` implementation via `CirreumAuthorizationBuilder`:

```csharp
builder.AddAuthorization(auth => auth
    .AddRoleResolver<MyRoleResolver>()
);
```

The resolver is called once per request during claims transformation. Requests that already carry role claims (e.g., workforce tokens) are skipped automatically.

```csharp
public class MyRoleResolver : IRoleResolver {
    public async Task<IReadOnlyList<string>?> ResolveRolesAsync(
        string externalUserId,
        CancellationToken cancellationToken = default) {
        // Load roles from your database
        return await db.GetRolesForUserAsync(externalUserId, cancellationToken);
    }
}
```

### Diagnostics

The claims transformer stashes a `ClaimsTransformResult` in `HttpContext.Items` after every transformation pass. Useful for debugging authorization issues in middleware or diagnostic endpoints:

```csharp
if (httpContext.Items[ClaimsTransformResult.ItemsKey] is ClaimsTransformResult result) {
    // result.Outcome:       "RolesResolved", "AlreadyTransformed", "NoUserIdentifier", etc.
    // result.ResolverType:  "MyRoleResolver"
    // result.UserId:        The external user ID from the token
    // result.RoleClaimType: The claim type used for roles
    // result.RoleCount:     Number of roles added
}
```

Telemetry is produced via `System.Diagnostics` using the diagnostic name `Cirreum.AuthorizationProvider`. The OTel subscription is handled automatically by `CirreumAuthorizationBuilder.AddRoleResolver<T>()`.

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Runtime.AuthorizationProvider follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

Given its foundational role, major version bumps are rare and carefully considered.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*