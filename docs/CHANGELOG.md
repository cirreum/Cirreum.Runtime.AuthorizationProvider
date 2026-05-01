# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.19] - 2026-05-01

### Fixed

- **`AudienceProviderRoleClaimsTransformer` now defensively stamps the
  authenticated scheme key on `HttpContext.Items`.** The dynamic scheme
  forward selector in `Cirreum.Runtime.Authorization` is the primary writer
  of `__Cirreum_AuthenticatedScheme`, but routes wired to an explicit
  authentication scheme bypass the forward selector — leaving the key
  unset for downstream consumers (boundary resolver, application user
  resolver dispatcher, `UserAccessor`). The transformer now `TryAdd`s the
  key from the principal it already has, ensuring all downstream consumers
  see a populated value regardless of whether the request went through
  dynamic dispatch. `TryAdd` preserves the forward selector's value when
  both run.

  The literal value `"__Cirreum_AuthenticatedScheme"` matches
  `Cirreum.Security.AuthenticationContextKeys.AuthenticatedScheme` in
  `Cirreum.Core 5.0.1`. This package does not reference `Cirreum.Core`
  (intended to be standalone in non-Cirreum hosts), so the literal is
  duplicated locally with a comment pointing at the canonical const.
