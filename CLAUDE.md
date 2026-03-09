# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **Cirreum.Runtime.AuthorizationProvider**, a .NET 10 library that provides authorization provider registration and role enrichment for the Cirreum Runtime Server. It's part of the Cirreum Foundation Framework ecosystem.

## Architecture

### Core Components

- **HostApplicationBuilderExtensions** (`Extensions/Hosting/`) — `RegisterAuthorizationProvider<TRegistrar, TSettings, TInstanceSettings>()` extension method for provider registration
- **AudienceProviderRoleClaimsTransformer** — `IClaimsTransformation` implementation that resolves application roles via `IRoleResolver` and adds them as `ClaimTypes.Role` claims. Internal.
- **ClaimsTransformResult** — Public diagnostic record stashed in `HttpContext.Items` after each transformation pass. Key: `ClaimsTransformResult.ItemsKey`.
- **AuthorizationProviderDiagnostics** — Internal `ActivitySource` and `Meter` using BCL `System.Diagnostics`. OTel subscription is handled by `Cirreum.Runtime.Authorization`.
- **RoleEnrichmentServiceCollectionExtensions** — `AddRoleEnrichment()` extension called by `CirreumAuthorizationBuilder.AddRoleResolver<T>()`.

### Project Structure

```
src/Cirreum.Runtime.AuthorizationProvider/
├── Extensions/
│   ├── Hosting/
│   │   └── HostApplicationBuilderExtensions.cs     # Provider registration
│   └── ServiceCollectionExtensions.cs              # AddRoleEnrichment()
├── AudienceProviderRoleClaimsTransformer.cs        # IClaimsTransformation (internal)
├── AuthorizationProviderDiagnostics.cs             # ActivitySource + Meter (internal)
├── ClaimsTransformResult.cs                        # Diagnostic record (public)
└── Cirreum.Runtime.AuthorizationProvider.csproj
```

### Layer Responsibilities

- **Cirreum.AuthorizationProvider** (Core) — Contracts: `IRoleResolver`, `AuthorizationDiagnostics.DiagnosticName` const, base registrar classes
- **Cirreum.Runtime.AuthorizationProvider** (Runtime) — Implementation: transformer, diagnostics, registration extension
- **Cirreum.Runtime.Authorization** (Runtime Extensions) — Composition: `AddRoleResolver<T>()` calls `AddRoleEnrichment()` + subscribes OTel

### Configuration Pattern

The library uses a hierarchical configuration pattern:
- Configuration path: `Cirreum:{ProviderType}:Providers:{ProviderName}`
- Supports multiple provider instances per registrar
- Validates configuration existence and binding before registration

## Development Commands

```bash
dotnet build
dotnet pack
dotnet clean
```

## Key Dependencies

- **Microsoft.AspNetCore.App**: Framework reference for ASP.NET Core integration
- **Cirreum.Logging.Deferred**: Structured logging with deferred execution
- **Cirreum.AuthorizationProvider**: Core authorization provider abstractions (IRoleResolver, base registrars)

## Build Configuration

- **Target Framework**: .NET 10.0
- **Language Version**: Latest C#
- **Nullable**: Enabled
- **Implicit Usings**: Enabled
- **Documentation**: XML documentation file generation enabled
- **Local Release Version**: 1.0.100-rc
- **VersionSuffix**: Uses `-rc` prefix (not `rc`) for valid SemVer
