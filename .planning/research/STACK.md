# Stack Research

**Domain:** .NET 10 Web API + Vue 3 SPA — OAuth2 Authorization Code Flow admin POC
**Researched:** 2026-03-11
**Confidence:** HIGH (all versions verified against NuGet/npm registries as of research date)

---

## Recommended Stack

### Backend — Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET 10 | 10.0.4 (LTS) | Runtime and SDK | LTS release (Nov 2025, supported until Nov 2028); latest performance and AOT improvements. Non-negotiable per project constraints. |
| ASP.NET Core 10 | 10.0.4 | Web API + Razor Pages host | Unified host for REST endpoints, OpenIddict server, and the single Razor login page. |
| OpenIddict | 7.3.0 | OAuth2/OIDC server | Version 7 (released July 2025) is the current stable line; 6.x is end-of-life and receives no security fixes. Trimming/AOT-compatible. |
| OpenIddict.AspNetCore | 7.3.0 | ASP.NET Core middleware integration | Required for integrating OpenIddict server/validation with ASP.NET Core request pipeline. |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.4 | User and role storage | Targets .NET 10 specifically; includes `IdentityDbContext` and all user/role infrastructure. |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.4 | In-memory database provider | Matches EF Core 10 LTS; correct POC choice — no persistence needed, simple setup. |
| Microsoft.AspNetCore.OpenApi | 10.0.4 | Built-in OpenAPI document generation | Native .NET 10 first-party package; replaces Swashbuckle in new templates from .NET 9+. No extra dependencies. |
| Scalar.AspNetCore | 2.13.5 | Developer API UI (dev-only) | Modern replacement for Swagger UI; pairs with `Microsoft.AspNetCore.OpenApi`; no dependencies. Use in Development only. |

### Frontend — Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| Vue 3 | 3.5.29 (stable) | SPA framework | Current stable; 3.6 beta in progress but 3.5.x is the production line as of March 2026. Non-negotiable. |
| Vite | 7.x (latest) | Build tool and dev server | Current major version; 20-30x faster TypeScript transpilation than tsc; standard for Vue 3 projects in 2025/2026. |
| TypeScript | 5.x (bundled with Vite) | Type safety | Official Vue recommendation; vue-tsc for SFC type checking. |
| PrimeVue | 4.5.4 | UI component library | Non-negotiable per project constraints; version 4 is the current generation with updated theming API. Use the free tier (Aura/Lara themes). |
| Pinia | 3.0.4 | State management | Official Vue state management; v3 drops Vue 2 support, simpler than Vuex, native Composition API integration. |
| Vue Router | 5.0.x | Client-side routing | The `vue-router` package is now at v5 (for Vue 3) as of 2026; same routing model as 4.x, just new major. |

### Frontend — OpenAPI Client Generation

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| @hey-api/openapi-ts | 0.93.x (pin exact) | TypeScript client codegen from OpenAPI spec | Active, well-adopted (Vercel, PayPal use it); supports OpenAPI 3.x; generates typed SDK, interfaces, and Fetch client. The predecessor `openapi-typescript-codegen` is abandoned. |

> **Important:** `@hey-api/openapi-ts` is pre-1.0 (initial development). Pin the exact version with `-E` flag when installing. API may change between minor versions.

---

## Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| OpenIddict.EntityFrameworkCore | 7.3.0 | EF Core store for OpenIddict applications/tokens | Required when using EF Core as the OpenIddict store backend (which this project does via Microsoft Identity's DbContext). |
| @primeicons/primeicons | latest | Icon set for PrimeVue | Required by most PrimeVue components that show icons; install alongside PrimeVue. |
| @primevue/themes | 4.5.4 | Preset themes (Aura, Lara, Nora) | Required for the new PrimeVue 4 theming API; choose Aura for a clean modern look. |

---

## Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| `dotnet` CLI | Create/build/run .NET projects | Use `dotnet new webapi` for the API project. |
| `vue-tsc` | Type-check Vue SFCs | Run in CI or as a pre-build step; Vite does not type-check by default. |
| Visual Studio 2026 / VS Code + Volar | IDE | VS Code + Vue - Official extension (Volar) is the standard for Vue 3 + TypeScript. VS 2026 ships with .NET 10 support. |
| `npx @hey-api/openapi-ts` | Generate TypeScript client | Run against the live API's `/openapi/v1.json` endpoint (provided by `Microsoft.AspNetCore.OpenApi`). |

---

## Installation

### .NET API Project

```bash
# Create Web API project
dotnet new webapi -n SimpleAdmin.Api
cd SimpleAdmin.Api

# OpenIddict (server + EF Core store + ASP.NET Core integration)
dotnet add package OpenIddict.AspNetCore --version 7.3.0
dotnet add package OpenIddict.EntityFrameworkCore --version 7.3.0

# Microsoft Identity + EF Core
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.4
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 10.0.4

# Built-in OpenAPI + Scalar UI
dotnet add package Microsoft.AspNetCore.OpenApi --version 10.0.4
dotnet add package Scalar.AspNetCore --version 2.13.5
```

### Vue SPA Project

```bash
# Scaffold with Vite (Vue + TypeScript template)
npm create vite@latest simple-admin-spa -- --template vue-ts
cd simple-admin-spa

# Core dependencies
npm install vue-router pinia

# PrimeVue
npm install primevue @primevue/themes primeicons

# Dev tools
npm install -D vue-tsc

# OpenAPI client generator (pin exact version — pre-1.0 package)
npm install -D -E @hey-api/openapi-ts
```

### Generate TypeScript Client

```bash
# Run after the API is running locally on :5000 (or wherever)
npx @hey-api/openapi-ts \
  -i http://localhost:5000/openapi/v1.json \
  -o src/client \
  -c @hey-api/client-fetch
```

---

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| OpenIddict 7.x | Duende IdentityServer | Commercial license required for production; overkill for a POC. OpenIddict is fully open source (Apache 2.0). |
| OpenIddict 7.x | OpenIddict 6.x | Never — 6.x is EOL and receives no security updates. |
| Microsoft.AspNetCore.OpenApi (built-in) | Swashbuckle.AspNetCore | Only if you need Swagger UI specifically; Swashbuckle is no longer in default templates for .NET 9/10. Would require adding v10 manually to be .NET 10 compatible. |
| Scalar.AspNetCore | Swagger UI (via Swashbuckle) | Scalar is simpler, zero-dependency, and works natively with `Microsoft.AspNetCore.OpenApi`. Use Swagger UI only if the team has a hard dependency on it. |
| @hey-api/openapi-ts | NSwag | NSwag can generate both the spec AND the client in one tool. Valid alternative if you prefer a single .NET-centric toolchain, but the generated TypeScript is less idiomatic. |
| @hey-api/openapi-ts | openapi-typescript-codegen | This package is abandoned by its author as of 2023. Do not use. |
| Pinia | Vuex | Vuex is legacy; Pinia is the official replacement. No reason to use Vuex with Vue 3 in 2026. |
| EF Core InMemory | SQLite in-memory | SQLite in-memory is more SQL-standard-compliant and better for relational testing, but adds a driver dependency. InMemory is simpler for a POC. |

---

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| OpenIddict 6.x | EOL as of July 2025; no security fixes. Active migration guide exists: `/guides/migration/60-to-70`. | OpenIddict 7.3.0 |
| Swashbuckle.AspNetCore (old versions) | Not compatible with .NET 10's `Microsoft.OpenApi v2` out of the box; requires v10 of Swashbuckle. Dropped from .NET templates. | `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` |
| openapi-typescript-codegen | Abandoned by author; unmaintained since 2023. | `@hey-api/openapi-ts` |
| Vuex | Legacy state management; replaced by Pinia officially. | Pinia 3.x |
| Vue CLI (`@vue/cli`) | Deprecated in 2023; Vite is the official successor. | Vite 7.x |
| `<Options API>` components | Project constraint requires Composition API throughout. Options API works but mixing styles adds cognitive overhead in a codebase that expects Composition API. | `<script setup>` with Composition API |

---

## Stack Patterns by Variant

**Single host (API serves both Razor login page and REST endpoints):**
- The API project should be an `ASP.NET Core Web API` project (not Minimal API — you need MVC for Razor Pages)
- Add `builder.Services.AddRazorPages()` and `app.MapRazorPages()` alongside `app.MapControllers()`
- This is the correct pattern for OpenIddict auth code flow — the token/auth endpoints are served by the same host as the login Razor page

**Authorization Code + PKCE for SPA:**
- Register the Vue SPA as a `public` client in OpenIddict (no client secret)
- Call `options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange()` on the server
- The Vue SPA handles the PKCE challenge itself (generate `code_verifier`, hash to `code_challenge`)
- Redirect URI must be the exact Vue dev server URL (e.g., `http://localhost:5173/callback`)

**OpenAPI document endpoint:**
- `Microsoft.AspNetCore.OpenApi` exposes the spec at `/openapi/v1.json` by default
- Map it with `app.MapOpenApi()` in `Program.cs`
- Point `@hey-api/openapi-ts` at this URL to regenerate the TypeScript client after API changes

---

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| OpenIddict 7.3.0 | .NET 8.0, 9.0, 10.0 | Multi-targets; uses `net8.0` TFM but runs on .NET 10. |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.4 | .NET 10.0 only | Strictly targets .NET 10; do not use with .NET 8/9 projects. |
| Microsoft.EntityFrameworkCore.InMemory 10.0.4 | EF Core 10.0.4 | Must match EF Core version; mixing minor versions causes restore failures. |
| Microsoft.AspNetCore.OpenApi 10.0.4 | .NET 10.0 | Built-in to framework version; always keep in sync with the SDK version. |
| Scalar.AspNetCore 2.13.5 | .NET 8.0, 9.0, 10.0 | No hard .NET version dependency; works across all supported .NET versions. |
| PrimeVue 4.5.4 | Vue 3.x | PrimeVue 4 requires Vue 3; not compatible with Vue 2. |
| Pinia 3.0.4 | Vue 3.x only | Pinia v3 drops Vue 2 support entirely. |
| @hey-api/openapi-ts 0.93.x | OpenAPI 2.0, 3.0, 3.1 | Pre-1.0; pin exact version. Regenerate client any time the API spec changes. |

---

## Sources

- NuGet Gallery: OpenIddict 7.3.0 — https://www.nuget.org/packages/OpenIddict (verified 2026-03-11) — HIGH confidence
- NuGet Gallery: OpenIddict.AspNetCore 7.3.0 — https://www.nuget.org/packages/OpenIddict.AspNetCore (verified 2026-03-11) — HIGH confidence
- OpenIddict 7.0 release blog: https://kevinchalet.com/2025/07/07/openiddict-7-0-is-out/ — confirms 6.x EOL — HIGH confidence
- OpenIddict migration guide 6→7: https://documentation.openiddict.com/guides/migration/60-to-70 — HIGH confidence
- NuGet: Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.4 — https://www.nuget.org/packages/Microsoft.AspNetCore.Identity.EntityFrameworkCore (verified 2026-03-11) — HIGH confidence
- NuGet: Microsoft.EntityFrameworkCore.InMemory 10.0.4 — https://www.nuget.org/packages/microsoft.entityframeworkcore.inmemory (verified 2026-03-11) — HIGH confidence
- NuGet: Microsoft.AspNetCore.OpenApi 10.0.4 — https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi (verified 2026-03-11) — HIGH confidence
- NuGet: Scalar.AspNetCore 2.13.5 — https://www.nuget.org/packages/Scalar.AspNetCore (verified 2026-03-11) — HIGH confidence
- .NET 10 GA announcement (November 2025): https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/ — HIGH confidence
- Microsoft Learn: Generate OpenAPI documents in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0 — HIGH confidence
- Vue 3.5.29 on npm (verified 2026-03-11) — latest stable line before 3.6 beta — HIGH confidence
- PrimeVue 4.5.4: https://www.npmjs.com/package/primevue (last published ~Jan 2026) — HIGH confidence
- Pinia 3.0.4: https://pinia.vuejs.org/ / npm (last published ~Nov 2025) — HIGH confidence
- Vue Router 5.0.x on npm (verified 2026-03-11) — HIGH confidence
- Vite 7.x: https://vite.dev/releases (verified 2026-03-11) — HIGH confidence
- @hey-api/openapi-ts 0.93.x: https://heyapi.dev/openapi-ts/get-started — MEDIUM confidence (pre-1.0, version may have advanced by implementation time — re-verify before use)
- OpenIddict PKCE docs: https://documentation.openiddict.com/configuration/proof-key-for-code-exchange — HIGH confidence

---

*Stack research for: SimpleAdmin — .NET 10 + OpenIddict + Vue 3 admin POC*
*Researched: 2026-03-11*
