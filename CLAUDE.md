# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **Cirreum.Runtime.AuthorizationProvider**, a .NET 10 library that provides authorization provider functionality for the Cirreum Runtime Server. It's part of the Cirreum Foundation Framework ecosystem and provides extension methods for registering authorization providers with ASP.NET Core applications.

## Architecture

### Core Components

- **HostApplicationBuilderExtensions**: The main entry point (`src/Cirreum.Runtime.AuthorizationProvider/Extensions/Hosting/HostApplicationBuilderExtensions.cs:24`) that provides the `RegisterAuthorizationProvider<TRegistrar, TSettings, TInstanceSettings>()` method
- **Authorization Provider Integration**: Integrates with the Cirreum.AuthorizationProvider package for provider registration and configuration
- **Deferred Logging**: Uses Cirreum.Logging.Deferred for structured logging during provider registration

### Project Structure

```
src/Cirreum.Runtime.AuthorizationProvider/
├── Extensions/Hosting/
│   └── HostApplicationBuilderExtensions.cs     # Main registration logic
├── Cirreum.Runtime.AuthorizationProvider.csproj
└── AUTHORIZATION.md                            # Authorization design documentation
```

### Configuration Pattern

The library uses a hierarchical configuration pattern:
- Configuration path: `Cirreum:{ProviderType}:Providers:{ProviderName}`
- Supports multiple provider instances per registrar
- Validates configuration existence and binding before registration

## Development Commands

### Build
```bash
dotnet build
```

### Package
```bash
dotnet pack
```

### Clean
```bash
dotnet clean
```

## Key Dependencies

- **Microsoft.AspNetCore.App**: Framework reference for ASP.NET Core integration
- **Cirreum.Logging.Deferred** (v1.0.102): Structured logging with deferred execution
- **Cirreum.AuthorizationProvider** (v1.0.1): Core authorization provider abstractions

## Build Configuration

- **Target Framework**: .NET 10.0
- **Language Version**: Latest C#
- **Nullable**: Enabled
- **Implicit Usings**: Enabled
- **Documentation**: XML documentation file generation enabled

### MSBuild Properties

The project uses a sophisticated build system with:
- **CI/CD Detection**: Automatically detects Azure DevOps, GitHub Actions, and general CI environments
- **Versioning**: Local builds default to 1.0.100-rc, CI builds use different versioning
- **Build Props**: Modular build configuration through separate .props files in `/build/`

## Authorization Design

See `AUTHORIZATION.md` for detailed authorization architecture including:
- OAuth2 scopes vs app roles distinction
- Delegated vs application permissions patterns
- Microsoft Entra ID/External ID integration patterns
- JWT token validation and role-based authorization

## Development Guidelines

Based on the project's contribution guidelines:

1. **Be conservative with new abstractions** - API surface must remain stable
2. **Limit dependency expansion** - Only add foundational, version-stable dependencies  
3. **Favor additive, non-breaking changes** - Breaking changes affect the entire ecosystem
4. **Include thorough unit tests** - All primitives should be independently testable
5. **Follow .NET conventions** - Use established patterns from Microsoft.Extensions.* libraries