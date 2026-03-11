---
phase: 01-backend-foundation
plan: 04
subsystem: testing
tags: [dotnet, xunit, integration-tests, openiddict, aspnetcore, webapplicationfactory, pkce, oauth2]

# Dependency graph
requires:
  - phase: 01-backend-foundation/01-03
    provides: AuthorizationController handling /connect/authorize and /connect/token, ApiController with /api/me, Login Razor page, seeded admin user and SPA client

provides:
  - xUnit integration test project (SimpleAdmin.Tests) with WebApplicationFactory
  - AuthFlowSmokeTests: authorize redirect, full Authorization Code + PKCE flow, token exchange, /api/me with Bearer token
  - ProtectedEndpointTests: GET /api/me without token returns 401
  - CookieTrackingHandler: helper for manual cookie tracking in WebApplicationFactory tests
  - All AUTH-01 and AUTH-03 requirements verified by automated tests

affects:
  - 02-frontend

# Tech tracking
tech-stack:
  added:
    - Microsoft.AspNetCore.Mvc.Testing 10.0.4 (xUnit integration testing)
    - xunit 2.9.3 (test framework)
  patterns:
    - WebApplicationFactory<Program> with UseEphemeralDataProtectionProvider for isolated test keys
    - CookieTrackingHandler delegating handler pattern for manual cookie tracking in TestServer
    - HandleCookies=true in WebApplicationFactoryClientOptions for automatic cookie container
    - SetDestinations() on ClaimsIdentity to control which claims appear in OpenIddict tokens
    - identity.SetDestinations(claim => ...) functional pattern for per-claim destinations

key-files:
  created:
    - SimpleAdmin.Tests/SimpleAdmin.Tests.csproj
    - SimpleAdmin.Tests/Helpers/TestWebApplicationFactory.cs
    - SimpleAdmin.Tests/Helpers/CookieTrackingHandler.cs
    - SimpleAdmin.Tests/AuthFlowSmokeTests.cs
    - SimpleAdmin.Tests/ProtectedEndpointTests.cs
    - SimpleAdmin.Api/Pages/_ViewImports.cshtml
  modified:
    - SimpleAdmin.Api/Controllers/AuthorizationController.cs
    - SimpleAdmin.Api/Workers/OpenIddictWorker.cs
    - SimpleAdmin.Api/Program.cs
    - SimpleAdmin.slnx

key-decisions:
  - "Use IdentityConstants.ApplicationScheme (Identity.Application) not CookieAuthenticationDefaults.AuthenticationScheme (Cookies) — these use different cookie names"
  - "Use explicit Redirect() not Challenge(Cookie) in authorize controller — Challenge inside OpenIddict passthrough returns 401 not 302"
  - "Set OpenIddict claim destinations via identity.SetDestinations(Func selector) — claims without destinations are dropped from tokens in v7"
  - "Register email and profile scopes in OpenIddict scope store via IOpenIddictScopeManager.CreateAsync — scope validation requires scope store entries even when client has permissions"
  - "Add scp:openid permission (Permissions.Prefixes.Scope + openid) to SPA client — required even though openid is OIDC built-in"
  - "Add _ViewImports.cshtml with @addTagHelper to Pages folder — required for antiforgery token injection in form tag helpers"
  - "public partial class Program {} in Program.cs — makes Program type visible to WebApplicationFactory in test project"

patterns-established:
  - "Integration test pattern: CookieTrackingHandler wrapping TestServer.CreateHandler() for manual cookie management across redirect chains"
  - "OpenIddict authorize flow: authenticate with IdentityConstants.ApplicationScheme, build ClaimsIdentity with SetDestinations, SetScopes, SignIn with OpenIddict scheme"

requirements-completed: [AUTH-01, AUTH-03]

# Metrics
duration: 30min
completed: 2026-03-11
---

# Phase 1 Plan 04: Integration Smoke Tests for Auth Code Flow Summary

**xUnit integration tests proving full Authorization Code + PKCE flow end-to-end with 3 bugs auto-fixed in the authorization controller and worker seeder**

## Performance

- **Duration:** 30 min
- **Started:** 2026-03-11T10:07:51Z
- **Completed:** 2026-03-11T10:38:28Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments

- Created `SimpleAdmin.Tests` xUnit project with `TestWebApplicationFactory<Program>` using Microsoft.AspNetCore.Mvc.Testing 10.0.4
- All 3 smoke tests pass: `AuthorizeEndpoint_RedirectsToLogin_WhenUnauthenticated`, `FullAuthCodeFlow_ReturnsAccessToken`, and `GetMe_WithoutToken_Returns401`
- Discovered and auto-fixed 5 bugs in the implementation that would have caused test failures and real-world auth failures

## Task Commits

Each task was committed atomically:

1. **Task 1: Create xUnit test project with TestWebApplicationFactory** - `c30ab13` (feat)
2. **Task 2: Create integration tests for auth flow and protected endpoint** - `887c0ba` (feat)

**Plan metadata:** (to be committed after SUMMARY)

## Files Created/Modified

- `SimpleAdmin.Tests/SimpleAdmin.Tests.csproj` - xUnit project targeting net10.0 with Mvc.Testing and AspNetCore.App FrameworkReference
- `SimpleAdmin.Tests/Helpers/TestWebApplicationFactory.cs` - WebApplicationFactory<Program> with Development environment
- `SimpleAdmin.Tests/Helpers/CookieTrackingHandler.cs` - DelegatingHandler that manually tracks Set-Cookie/Cookie headers for TestServer
- `SimpleAdmin.Tests/AuthFlowSmokeTests.cs` - Full PKCE auth code flow test: authorize redirect, login, code exchange, /api/me
- `SimpleAdmin.Tests/ProtectedEndpointTests.cs` - GET /api/me without Bearer token returns 401
- `SimpleAdmin.Api/Pages/_ViewImports.cshtml` - Tag helper registration enabling antiforgery token injection in Razor forms
- `SimpleAdmin.Api/Controllers/AuthorizationController.cs` - Fixed authentication scheme, redirect method, and claim destinations
- `SimpleAdmin.Api/Workers/OpenIddictWorker.cs` - Added scope store entries and openid permission to SPA client
- `SimpleAdmin.Api/Program.cs` - Added `public partial class Program {}` for WebApplicationFactory visibility
- `SimpleAdmin.slnx` - Added SimpleAdmin.Tests project

## Decisions Made

- Used `IdentityConstants.ApplicationScheme` ("Identity.Application") in `AuthorizationController.Authorize()` instead of `CookieAuthenticationDefaults.AuthenticationScheme` ("Cookies") — they use different cookie names: `.AspNetCore.Identity.Application` vs `.AspNetCore.Cookies`
- Used explicit `Redirect("/Account/Login?ReturnUrl=...")` instead of `Challenge(Cookie)` — Challenge inside OpenIddict's passthrough authorize endpoint returns HTTP 401 with Location header, not HTTP 302
- Applied `identity.SetDestinations(claim => ...)` pattern to all claims — in OpenIddict v7, claims without destinations are silently dropped from access tokens (sub has implicit destination, others do not)
- Added email/profile scopes to the OpenIddict scope store — ID2052 error appears if client has scope permissions but scopes aren't registered in the scope store
- Added `scp:openid` permission to SPA client — required in OpenIddict v7 even for the OIDC built-in openid scope

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] AuthorizationController used wrong cookie auth scheme**
- **Found during:** Task 2 (FullAuthCodeFlow test)
- **Issue:** Controller called `AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)` ("Cookies") which looks for `.AspNetCore.Cookies`, but `SignInManager.PasswordSignInAsync` uses `IdentityConstants.ApplicationScheme` ("Identity.Application") which creates `.AspNetCore.Identity.Application`. Authentication always returned `NoResult` even with a valid cookie.
- **Fix:** Changed to `AuthenticateAsync(IdentityConstants.ApplicationScheme)` and added `using Microsoft.AspNetCore.Identity`
- **Files modified:** SimpleAdmin.Api/Controllers/AuthorizationController.cs
- **Verification:** Server-side log showed `Succeeded=True` after fix; authorize redirects to callback with code
- **Committed in:** 887c0ba (Task 2 commit)

**2. [Rule 1 - Bug] Challenge(Cookie) inside OpenIddict passthrough returns 401 not 302**
- **Found during:** Task 2 (AuthorizeEndpoint_RedirectsToLogin test)
- **Issue:** `return Challenge(CookieAuthenticationDefaults, ...)` inside OpenIddict's passthrough authorize endpoint returns HTTP 401 with a Location header instead of the expected HTTP 302 redirect. The test asserted `HttpStatusCode.Redirect` but received `Unauthorized`.
- **Fix:** Changed to `return Redirect($"/Account/Login?ReturnUrl=...")` which guarantees HTTP 302
- **Files modified:** SimpleAdmin.Api/Controllers/AuthorizationController.cs
- **Verification:** Test `AuthorizeEndpoint_RedirectsToLogin_WhenUnauthenticated` now passes
- **Committed in:** 887c0ba (Task 2 commit)

**3. [Rule 1 - Bug] Email claim missing from OpenIddict access token**
- **Found during:** Task 2 (FullAuthCodeFlow /api/me assertion)
- **Issue:** `/api/me` returned `{"sub":"...","email":null}` because OpenIddict v7 drops claims without explicit destinations from tokens. `identity.SetClaim(Claims.Email, email)` alone is not enough.
- **Fix:** Added `identity.SetDestinations(claim => ...)` after building the identity to assign `AccessToken` destination to all claims
- **Files modified:** SimpleAdmin.Api/Controllers/AuthorizationController.cs
- **Verification:** JWT token payload includes `"email":"admin@simpleadmin.local"`; /api/me returns 200 with email
- **Committed in:** 887c0ba (Task 2 commit)

**4. [Rule 1 - Bug] OpenIddict scope validation rejected email/profile scopes**
- **Found during:** Task 2 (authorize URL with scope=openid email profile returned 400 invalid_scope)**
- **Issue:** OpenIddict error ID2052 rejected `email` and `profile` scopes. Client had `scp:email` and `scp:profile` permissions but the scopes were not registered in the scope store. OpenIddict v7 requires scope store entries for all non-built-in scopes.
- **Fix:** Added `IOpenIddictScopeManager.CreateAsync` calls in `OpenIddictWorker.StartAsync` for email and profile scopes
- **Files modified:** SimpleAdmin.Api/Workers/OpenIddictWorker.cs
- **Verification:** Authorize request with scope=openid email profile returns 302 to login
- **Committed in:** 887c0ba (Task 2 commit)

**5. [Rule 1 - Bug] Missing _ViewImports.cshtml prevented tag helper rendering**
- **Found during:** Task 2 (login POST returned 400 antiforgery validation failure)
- **Issue:** Without `_ViewImports.cshtml`, Razor tag helpers were not active. The `<form method="post">` and `<input asp-for="...">` tags were rendered as raw HTML without the `__RequestVerificationToken` hidden field. Login POST returned 400 Bad Request (antiforgery failure).
- **Fix:** Created `SimpleAdmin.Api/Pages/_ViewImports.cshtml` with `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`
- **Files modified:** SimpleAdmin.Api/Pages/_ViewImports.cshtml (created)
- **Verification:** Login page HTML contains `__RequestVerificationToken` hidden input; login POST succeeds
- **Committed in:** 887c0ba (Task 2 commit)

---

**Total deviations:** 5 auto-fixed (5 bugs — all correctness issues in auth flow implementation)
**Impact on plan:** All auto-fixes were necessary for correct auth flow behavior. Without these fixes, the auth flow would have been broken in production too. No scope creep.

## Issues Encountered

- Pre-existing NU1900 warnings from private Azure DevOps NuGet feeds (iboris) — unrelated, present in all builds
- OpenIddict v7 requires `scp:openid` permission explicitly in client even for built-in OIDC scope
- `WebApplicationFactory` `HandleCookies=true` uses `CookieContainerHandler` wrapping `TestServer.CreateHandler()` — cookie tracking works but required careful investigation to determine correct auth scheme

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All AUTH-01 and AUTH-03 requirements verified by automated tests running with `dotnet test`
- Full Authorization Code + PKCE flow proven end-to-end: GET /connect/authorize → /Account/Login → POST credentials → GET /connect/authorize (with cookie) → 302 to callback with code → POST /connect/token → GET /api/me with Bearer token
- Phase 1 (Backend Foundation) is complete — Phase 2 (Frontend) can begin

## Self-Check: PASSED

All created files exist, all commits verified, all 3 tests pass (0 failures).

---
*Phase: 01-backend-foundation*
*Completed: 2026-03-11*
