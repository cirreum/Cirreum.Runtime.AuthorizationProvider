# Cirreum.Runtime.AuthorizationProvider

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Runtime.AuthorizationProvider.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.AuthorizationProvider/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Runtime.AuthorizationProvider.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Runtime.AuthorizationProvider/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Runtime.AuthorizationProvider?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Runtime.AuthorizationProvider/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Runtime.AuthorizationProvider?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Runtime.AuthorizationProvider/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Authorization provider extensions for the Cirreum Runtime Server**

## Overview

**Cirreum.Runtime.AuthorizationProvider** provides extension methods for registering authorization providers with ASP.NET Core applications in the Cirreum Runtime Server ecosystem. It streamlines the configuration and registration of authorization providers through a type-safe, configuration-driven approach.

## Features

- **Type-safe provider registration** with generic constraints
- **Configuration-driven setup** using hierarchical configuration sections
- **Multiple provider instances** support per registrar type
- **Duplicate registration protection** with automatic detection
- **Structured logging** with deferred execution for performance
- **Seamless ASP.NET Core integration** through `IHostApplicationBuilder` extensions

## Installation

```bash
dotnet add package Cirreum.Runtime.AuthorizationProvider
```

## Usage

### Basic Registration

```csharp
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var authBuilder = builder.Services.AddAuthentication();

// Register your authorization provider
builder.RegisterAuthorizationProvider<MyAuthRegistrar, MyAuthSettings, MyInstanceSettings>(authBuilder);

var app = builder.Build();
```

### Configuration Structure

Configure your authorization providers in `appsettings.json`:

```json
{
  "Cirreum": {
    "Authorization": {
      "Providers": {
        "MyProvider": {
          "Instances": [
            {
              "Name": "Primary",
              "Endpoint": "https://auth.example.com",
              "ClientId": "your-client-id"
            }
          ]
        }
      }
    }
  }
}
```

### Custom Authorization Provider

Implement your authorization provider by inheriting from the base classes:

```csharp
public class MyAuthRegistrar : AuthorizationProviderRegistrar<MyAuthSettings, MyInstanceSettings>
{
    public override ProviderType ProviderType => ProviderType.Authorization;
    public override string ProviderName => "MyProvider";
    
    public override void Register(
        IServiceCollection services, 
        MyAuthSettings settings, 
        IConfigurationSection section,
        AuthenticationBuilder authBuilder)
    {
        // Your registration logic here
    }
}
```

## Documentation

- **[Authorization Design](AUTHORIZATION.md)** - Comprehensive guide to OAuth2 scopes, app roles, and Microsoft Entra ID integration patterns

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