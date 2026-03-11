---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
current_phase: 1
current_phase_name: Backend Foundation
current_plan: 4
status: executing
stopped_at: Phase 4 context gathered
last_updated: "2026-03-11T13:46:07.860Z"
last_activity: 2026-03-11
progress:
  total_phases: 4
  completed_phases: 3
  total_plans: 8
  completed_plans: 8
  percent: 0
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-11)

**Core value:** A working end-to-end auth code flow login that lands in a Vue 3 SPA where you can list, create, edit, and delete users.
**Current focus:** Phase 1 — Backend Foundation

## Current Position

Current Phase: 1
Total Phases: 4
Current Phase Name: Backend Foundation
Current Plan: 4
Total Plans in Phase: 4
Status: In progress
Last Activity: 2026-03-11

Progress: [░░░░░░░░░░] 0%

## Performance Metrics

**Velocity:**
- Total plans completed: 0
- Average duration: -
- Total execution time: 0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**
- Last 5 plans: none yet
- Trend: -

*Updated after each plan completion*
| Phase 01-backend-foundation P01 | 4min | 2 tasks | 6 files |
| Phase 01-backend-foundation P02 | 4min | 2 tasks | 2 files |
| Phase 01-backend-foundation P03 | 8min | 2 tasks | 4 files |
| Phase 01-backend-foundation P04 | 32min | 2 tasks | 10 files |
| Phase 02-rest-api-contract P01 | 8min | 2 tasks | 7 files |
| Phase 02-rest-api-contract P02 | 7min | 2 tasks | 2 files |
| Phase 03-vue-spa-oidc P01 | 6min | 2 tasks | 22 files |
| Phase 03-vue-spa-oidc P02 | 13min | 2 tasks | 13 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Phase 1]: Use `AddIdentity<ApplicationUser, IdentityRole>` (not `AddDefaultIdentity`) to avoid default UI conflicts with OpenIddict
- [Phase 1]: Pass shared `InMemoryDatabaseRoot` singleton to all `UseInMemoryDatabase()` calls to avoid data isolation across DbContext instances
- [Phase 1]: Use `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme` as default auth scheme, not JwtBearer
- [Phase 3]: Use `oidc-client-ts` with `sessionStorage` store (POC-appropriate); pin `@hey-api/openapi-ts` 0.93.x with `-E` flag
- [Phase 01-backend-foundation]: Added Microsoft.AspNetCore.Identity.EntityFrameworkCore explicitly — not pulled transitively by OpenIddict 7.3.0
- [Phase 01-backend-foundation]: InMemoryDatabaseRoot namespace is Microsoft.EntityFrameworkCore.Storage (not Infrastructure.Memory) in EF Core 10
- [Phase 01-backend-foundation]: OpenIddictConstants.ClientTypes/Permissions (not OpenIddict.Abstractions.ClientTypes) is the correct access pattern in OpenIddict 7.3.0
- [Phase 01-backend-foundation]: OpenIddictValidationAspNetCoreDefaults as DefaultScheme; Cookie as DefaultChallengeScheme only
- [Phase 01-backend-foundation]: OpenIddictConstants.Claims.* are the correct claim constants in v7.x (not OpenIddict.Abstractions.Claims.* which does not exist)
- [Phase 01-backend-foundation]: GetOpenIddictServerRequest extension method requires 'using Microsoft.AspNetCore' (class is in Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers)
- [Phase 01-backend-foundation]: Fully-qualified @model directive required in Login.cshtml without _ViewImports.cshtml: SimpleAdmin.Api.Pages.Account.LoginModel
- [Phase 01-backend-foundation]: Use IdentityConstants.ApplicationScheme not CookieAuthenticationDefaults.AuthenticationScheme — they use different cookie names (.AspNetCore.Identity.Application vs .AspNetCore.Cookies)
- [Phase 01-backend-foundation]: OpenIddict v7: set claim destinations with identity.SetDestinations(Func selector) — claims without destinations are dropped from access tokens
- [Phase 01-backend-foundation]: OpenIddict v7 scope validation requires scope store entries for email/profile via IOpenIddictScopeManager.CreateAsync
- [Phase 02-rest-api-contract]: Use synchronous .ToList() on UserManager.Users — no ToListAsync equivalent for LINQ projections over IdentityUser
- [Phase 02-rest-api-contract]: RemovePasswordAsync + AddPasswordAsync for admin password updates (not ChangePasswordAsync which requires current password)
- [Phase 02-rest-api-contract]: OpenAPI and Scalar endpoints registered only under IsDevelopment() guard
- [Phase 02-rest-api-contract]: TokenHelper receives HttpClient as parameter — caller controls AllowAutoRedirect=false + HandleCookies=true; TokenHelper does not create or configure client
- [Phase 02-rest-api-contract]: Use Guid.NewGuid() in test email addresses to prevent cross-test DB collisions on shared in-memory database
- [Phase 02-rest-api-contract]: AuthFlowSmokeTests left untouched — existing passing tests not refactored to use TokenHelper (out of scope for this plan)
- [Phase 03-vue-spa-oidc]: OpenIddict v7: SetEndSessionEndpointUris (not SetLogoutEndpointUris), EnableEndSessionEndpointPassthrough (not EnableLogoutEndpointPassthrough), Permissions.Endpoints.EndSession (not .Logout)
- [Phase 03-vue-spa-oidc]: Vue 3 SPA: app.use(pinia) MUST come before app.use(router); useAuthStore() called inside beforeEach body (not module top level) to avoid Pinia initialization error
- [Phase 03-vue-spa-oidc]: hey-api generated client embeds baseUrl from OpenAPI spec server URL — no explicit setConfig call needed in main.ts
- [Phase 03-vue-spa-oidc]: Backend runs on port 5009 (launchSettings.json); all SPA references updated: oidcManager authority, openapi-ts.config.ts input, playwright.config.ts webServer
- [Phase 03-vue-spa-oidc]: OpenIddict v7: ALL requested scopes (including 'api') must be registered via IOpenIddictScopeManager.CreateAsync — not just listed as permissions on the application

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-03-11T13:46:07.855Z
Stopped at: Phase 4 context gathered
Resume file: .planning/phases/04-user-crud-views/04-CONTEXT.md
