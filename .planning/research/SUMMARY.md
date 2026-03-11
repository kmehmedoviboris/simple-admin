# Project Research Summary

**Project:** SimpleAdmin
**Domain:** User management admin panel POC — .NET 10 Web API + OpenIddict + Vue 3 SPA (OAuth2 Authorization Code + PKCE)
**Researched:** 2026-03-11
**Confidence:** HIGH

## Executive Summary

SimpleAdmin is a proof-of-concept admin panel built around a .NET 10 Web API acting as both an OAuth2 Authorization Server (via OpenIddict 7.3.0) and a REST Resource Server, with a Vue 3 SPA as the sole consumer. The architecture is deliberately self-contained: a single .NET host serves the Razor login page, OIDC endpoints, and REST API; the Vue SPA performs PKCE-protected Authorization Code flow and calls the API with Bearer tokens issued by the same host. This "same-host" pattern is the correct and well-documented approach for a POC of this type — it avoids the operational complexity of a dedicated Authorization Server while demonstrating the full OAuth2 flow.

The recommended build order is strict: the data layer and OpenIddict server configuration must be complete before any auth flow can be tested, OIDC client integration in the Vue SPA must be validated before building CRUD views, and the OpenAPI contract (generated from the running API) bridges back-end and front-end work. All P1 features (login, logout, user list, create, edit, delete with confirmation, form validation, loading states, error feedback) are achievable at LOW implementation complexity; this is not a feature-scope risk but a wiring-complexity risk.

The dominant risk category is integration configuration, not business logic. At least 11 distinct pitfalls are documented, clustered around OpenIddict server setup (Phase 1). Getting the OpenIddict application descriptor right — public client type, exact redirect URIs, four permission categories, and scope registration — is the single most error-prone area and the most common source of "it almost works" failure modes. All pitfalls in this cluster have LOW recovery cost once identified, but are frequently encountered in sequence during initial server wiring, which can consume significant time if addressed ad-hoc rather than proactively.

## Key Findings

### Recommended Stack

The back end is .NET 10 (10.0.4 LTS) with ASP.NET Core 10, OpenIddict 7.3.0, Microsoft Identity + EF Core 10 (in-memory), and the built-in `Microsoft.AspNetCore.OpenApi` document generation with `Scalar.AspNetCore` as the developer UI. OpenIddict 7.x is the current stable line; 6.x is EOL and must not be used. The front end is Vue 3 (3.5.x stable), Vite 7.x, TypeScript, PrimeVue 4.5.4 (non-negotiable project constraint), Pinia 3.0.4, and Vue Router 5.x. The API contract is enforced via `@hey-api/openapi-ts` (pin exact pre-1.0 version) generating a typed TypeScript client from the running API's OpenAPI spec.

**Core technologies:**
- .NET 10 / ASP.NET Core 10: runtime and unified host for REST, OpenIddict, and Razor Pages — LTS until Nov 2028
- OpenIddict 7.3.0: OAuth2/OIDC Authorization Server — only current stable line; 6.x is EOL
- Microsoft Identity + EF Core InMemory 10.0.4: user storage and in-process data — matched versions are required
- Vue 3.5.29 + Vite 7.x: SPA framework and build tool — non-negotiable per project constraints
- PrimeVue 4.5.4: UI component library — non-negotiable; use Aura theme
- Pinia 3.0.4: state management — official Vue replacement for Vuex
- `@hey-api/openapi-ts` 0.93.x: TypeScript client codegen — pin exact version; predecessor library is abandoned
- `oidc-client-ts`: PKCE dance and token management in the Vue SPA — configure with `sessionStorage` for POC

### Expected Features

**Must have (table stakes — P1, all LOW complexity):**
- Login via Authorization Code + PKCE — nothing else is accessible without this
- Logout — clear tokens, redirect to login
- User list view — landing page after login; PrimeVue DataTable
- Create user (email + password) — core CRUD
- Edit user (email; optional password update) — core CRUD
- Delete user with confirmation dialog — destructive action requires friction
- Form validation feedback — inline errors for required fields, email format, password minimum
- Loading states on list and form submissions — UX hygiene
- Error feedback on API failure — toast or alert on 4xx/5xx

**Should have (P2, add after POC validates):**
- Search / quick-filter on email
- Column sorting
- Pagination (server-side; only relevant when in-memory is replaced)
- User status (active/inactive) toggle — avoids hard deletes in production

**Defer (v2+):**
- Role management UI — doubles scope; hard-code single admin role for POC
- Audit log — build only when compliance requirements exist
- Bulk delete
- Two-factor authentication
- Password reset / email verification — no email infrastructure in POC

**Anti-features to explicitly avoid:**
- Real-time updates (WebSocket/SSE) — no POC value
- Dashboard / analytics — no meaningful data source
- Mobile responsiveness — desktop-only is in-scope constraint
- Persistent database — in-memory is the stated constraint

### Architecture Approach

The architecture is a single-host .NET 10 project serving three concerns from one process: a Razor login page for credential collection, OpenIddict OIDC endpoints (`/connect/authorize`, `/connect/token`, `/connect/logout`), and REST controllers (`/api/users`). The Vue SPA is a separate Vite project that treats the .NET host purely as an external service. The `AuthorizationController` implements OpenIddict passthrough mode — it validates the OIDC request, checks for a pre-existing Identity cookie, and either redirects to the login page or issues the authorization code. This separation of concerns (login page vs. OIDC controller) is the critical architectural pattern and must not be collapsed.

**Major components:**
1. Vue 3 SPA — all admin UI, token management via `oidc-client-ts`, API consumption via generated typed client
2. Razor Login Page (`/Account/Login`) — server-rendered credential collection; sets Identity application cookie
3. `AuthorizationController` — OpenIddict passthrough; reads cookie, builds claims principal, returns auth code
4. `UsersController` — REST CRUD endpoints protected by `[Authorize]` with OpenIddict validation scheme
5. OpenIddict Server — issues authorization codes, access tokens, refresh tokens; configured in `Program.cs`
6. `ApplicationDbContext` — single EF Core context inheriting `IdentityDbContext<ApplicationUser>` with `options.UseOpenIddict()`; shared by Identity and OpenIddict
7. `OpenIddictWorker` (IHostedService) — seeds the SPA client application descriptor before any request is accepted
8. Generated API client + auth interceptor — `@hey-api/openapi-ts` output wrapped in `api/client.ts` that injects Bearer tokens

### Critical Pitfalls

1. **SPA registered as confidential client instead of public** — Always set `ClientType = OpenIddictConstants.ClientTypes.Public` and call `RequireProofKeyForCodeExchange()`. Never assign a `ClientSecret` to the SPA application descriptor.

2. **Exact redirect_uri mismatch (error ID2043)** — The URI registered in the `OpenIddictApplicationDescriptor` must exactly match what `oidc-client-ts` sends, including scheme, port, path, and trailing slash. Register both HTTP and HTTPS localhost variants. Watch for Vite shifting from port 5173 to 5174 when port is occupied.

3. **Incomplete application permissions on the descriptor** — All four permission categories must be explicitly set: endpoint access (`Authorization`, `Token`), grant type (`AuthorizationCode`), response type (`Code`), and scopes (`Profile`, `Email`, plus any custom scope prefixed with `Permissions.Prefixes.Scope`). Server-level `AllowAuthorizationCodeFlow()` does not substitute for per-application permissions.

4. **CORS middleware ordering** — `app.UseCors()` must be placed before `app.UseAuthentication()` and `app.UseAuthorization()` in the pipeline. Incorrect ordering causes 401 responses to omit CORS headers, making authentication failures appear as CORS failures and making debugging misleading.

5. **OpenIddict validation not using local server** — The validation stack must call `options.UseLocalServer()`. Without it, `[Authorize]` controllers reject all tokens with 401 even when the tokens are valid. Use `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme` as the default authentication scheme, not `JwtBearer`.

6. **In-memory EF data isolation across DbContext instances** — Pass a shared `InMemoryDatabaseRoot` singleton to all `UseInMemoryDatabase()` calls. Without this, the seeding service and request-handling contexts access different in-memory stores, causing OpenIddict to report "application not found" on auth requests.

7. **Scope not registered server-wide** — Custom scopes must be registered via `options.RegisterScopes(...)` on the OpenIddict server in addition to being granted as permissions on the application descriptor. Omitting this causes `invalid_scope` at the token endpoint.

## Implications for Roadmap

Based on research, the build order has hard dependencies that should directly map to phases. The architecture research provides a 10-step suggested build order that aligns tightly with where pitfalls cluster.

### Phase 1: Backend Foundation and Authorization Server

**Rationale:** Everything else depends on a working auth flow. The seven OpenIddict-cluster pitfalls (Pitfalls 1–4, 7, 9, and the validation pitfall) all manifest here. Address them proactively rather than reactively. This phase has no UI output — it produces a working OIDC server testable via browser redirect and a curl token exchange.

**Delivers:**
- `ApplicationDbContext` with Identity + OpenIddict tables (in-memory, shared root)
- `Program.cs` with complete DI wiring: Identity, OpenIddict server, CORS, OpenAPI
- `OpenIddictWorker` seeding the SPA client application descriptor (public, PKCE, all permissions)
- `AuthorizationController` (passthrough mode)
- Razor Login Page (`/Account/Login`)
- Verified end-to-end auth code flow (manual browser test or integration test)

**Must avoid:** Pitfalls 1–9 from PITFALLS.md (all Phase 1 pitfalls — confidential client, redirect URI mismatch, missing permissions, missing scope registration, CORS ordering, missing `UseLocalServer()`, Razor Pages routing, in-memory data isolation)

**Research flag:** This phase requires careful adherence to the pitfall checklist. The patterns are well-documented in OpenIddict official docs; however, implementation complexity is high. Consider using the "Looks Done But Isn't" checklist from PITFALLS.md as a phase exit gate.

### Phase 2: REST API and OpenAPI Contract

**Rationale:** Once auth works end-to-end, `UsersController` can be built against the verified Identity + OpenIddict stack. The OpenAPI spec is the handoff artifact for front-end work; generating it ends Phase 2 and enables Phase 3 to start.

**Delivers:**
- `UsersController` with GET, POST, PUT, DELETE endpoints protected by `[Authorize]`
- Request/response DTOs
- Startup seed of admin user and test users
- OpenAPI spec generated from the running API (`/openapi/v1.json`)

**Implements:** `UsersController`, `UserManager<ApplicationUser>` integration
**Avoids:** Pitfall 10 (OpenAPI client generation) — generate against a running server, not a build-time artifact

**Research flag:** Standard REST + MVC patterns; no additional research needed.

### Phase 3: Vue SPA Foundation and OIDC Integration

**Rationale:** SPA scaffolding and OIDC wiring must come before CRUD views. Verify the full login round-trip in the SPA before building any views — a broken auth loop discovered during CRUD work is significantly more expensive to diagnose.

**Delivers:**
- Vite + Vue 3 + TypeScript + PrimeVue project scaffolded
- `oidc-client-ts` configured with `sessionStorage` store (POC-appropriate)
- `authStore` (Pinia) for reactive auth state
- Vue Router with auth guard
- OIDC callback route that calls `signinRedirectCallback()` then redirects to `/users`
- `api/client.ts` wrapping the generated client with Bearer token interceptor
- Verified login and logout round-trip from the SPA

**Avoids:** Pitfall 10 (token interceptor wiring), Pitfall 11 (session lost on page refresh), UX pitfall (redirect loop after callback)

**Research flag:** `oidc-client-ts` integration is well-documented; pattern is established. No additional research needed.

### Phase 4: User CRUD Views

**Rationale:** All dependencies are in place. Views are the final layer and the only phase with user-visible UI. Feature complexity is LOW for all P1 items.

**Delivers:**
- `UserListView.vue` — PrimeVue DataTable with email column, loading state, error feedback
- `UserCreateView.vue` / `UserEditView.vue` — form with validation, loading state, error feedback
- `UserForm.vue` — shared component reused by create and edit
- PrimeVue `ConfirmDialog` for delete confirmation
- Toast notifications for API errors and success feedback
- Logout functionality clearing tokens and redirecting to login

**Features addressed:** All P1 features from FEATURES.md
**Uses:** PrimeVue DataTable, ConfirmDialog, Toast; generated API client; Pinia authStore

**Research flag:** PrimeVue 4 component usage is well-documented; DataTable patterns are standard. No additional research needed.

### Phase Ordering Rationale

- Phase 1 before Phase 2: `UsersController` requires OpenIddict validation stack to be configured for `[Authorize]` to work.
- Phase 2 before Phase 3: The generated TypeScript client requires the running API's OpenAPI spec; generating it ends Phase 2.
- Phase 3 before Phase 4: CRUD views import from the generated client and read from the auth store; both must exist and be tested before views are built.
- OIDC verified before CRUD views: A broken auth loop found during CRUD work is harder to isolate than one found with a minimal SPA scaffold.

### Research Flags

Phases needing additional research during planning:
- **Phase 1 (Authorization Server setup):** High integration complexity with multiple interacting subsystems (OpenIddict + Identity + EF Core + Razor Pages in one host). The patterns are documented but the pitfall surface is wide. Use PITFALLS.md "Looks Done But Isn't" checklist as the exit gate. No additional `/gsd:research-phase` needed — PITFALLS.md and ARCHITECTURE.md are comprehensive.

Phases with standard patterns (skip research-phase):
- **Phase 2 (REST API):** Standard ASP.NET Core controller + EF Core + `UserManager` patterns. No novel integration.
- **Phase 3 (Vue OIDC):** `oidc-client-ts` with `sessionStorage` is a documented POC pattern. ARCHITECTURE.md provides exact configuration examples.
- **Phase 4 (CRUD views):** PrimeVue DataTable + forms are standard; no research needed.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All versions verified against NuGet and npm registries as of 2026-03-11. Only caveat: `@hey-api/openapi-ts` is pre-1.0 — re-verify version at implementation time. |
| Features | HIGH | Feature set is small, well-defined, and maps directly to stated POC constraints. No ambiguity. |
| Architecture | HIGH | Architecture patterns sourced from official OpenIddict docs and verified sample projects. Passthrough mode and shared DbContext patterns are explicitly documented. |
| Pitfalls | HIGH | 11 pitfalls with code-level prevention strategies, all verified against official docs and GitHub issues. |

**Overall confidence:** HIGH

### Gaps to Address

- **`@hey-api/openapi-ts` version:** Pre-1.0 package; pin exact version with `-E` at install time and re-verify the latest 0.93.x release before scaffolding the front end. API may have changed between research date and implementation.
- **`oidc-client-ts` version:** Not pinned in STACK.md — confirm the latest stable version at implementation time and add to `package.json`.
- **OpenIddict 7.x + .NET 10 runtime compatibility note:** OpenIddict 7.3.0 multi-targets `net8.0` but runs on .NET 10. Confirm no runtime warnings appear at startup; this is expected behavior per the official release notes.
- **`AddDefaultIdentity` vs `AddIdentity` choice:** PITFALLS.md flags that `AddDefaultIdentity` registers a default UI that conflicts with OpenIddict. Use `AddIdentity<ApplicationUser, IdentityRole>` with explicit cookie configuration instead. Confirm this during Phase 1 wiring.

## Sources

### Primary (HIGH confidence)
- NuGet Gallery — OpenIddict 7.3.0, Identity 10.0.4, EF Core InMemory 10.0.4, Scalar 2.13.5, Microsoft.AspNetCore.OpenApi 10.0.4 (verified 2026-03-11)
- https://documentation.openiddict.com — server setup, passthrough mode, application permissions, PKCE, EF Core integration, scope registration
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0 — built-in OpenAPI generation
- https://authts.github.io/oidc-client-ts/ — PKCE client configuration and sessionStorage store
- https://openapi-ts.dev/openapi-fetch/middleware-auth — Bearer token interceptor pattern
- https://documentation.openiddict.com/integrations/entity-framework-core — shared DbContext pattern

### Secondary (MEDIUM confidence)
- https://dev.to/robinvanderknaap/setting-up-an-authorization-server-with-openiddict-part-iv-authorization-code-flow-3eh8 — controller structure and data flow walkthrough
- https://andreyka26.com/oauth-authorization-code-using-openiddict-and-dot-net — Razor Page + OpenIddict login integration
- https://github.com/eamsdev/OpenIddict-ReactSpa-WebApi-PoC — SPA + Web API separation pattern reference

### Tertiary (MEDIUM-LOW confidence)
- https://heyapi.dev/openapi-ts/get-started — pre-1.0 package; re-verify version before implementation
- GitHub OpenIddict issues #1554, #706, #684 — redirect URI and in-memory DB isolation pitfall confirmations

---
*Research completed: 2026-03-11*
*Ready for roadmap: yes*
