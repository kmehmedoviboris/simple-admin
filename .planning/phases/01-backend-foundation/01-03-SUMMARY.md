---
phase: 01-backend-foundation
plan: 03
subsystem: auth
tags: [dotnet, openiddict, oauth2, pkce, razorpages, identity, aspnetcore]

# Dependency graph
requires:
  - phase: 01-backend-foundation/01-02
    provides: OpenIddict server configured with passthrough mode, auth schemes, CORS, Razor Pages registration, seeded admin user and SPA client

provides:
  - AuthorizationController handling GET+POST /connect/authorize (cookie challenge redirect + ClaimsIdentity signing)
  - AuthorizationController handling POST /connect/token (authorization code exchange)
  - ApiController with GET /api/me protected by OpenIddict validation scheme
  - Razor login page (/Account/Login) with minimal centered CSS, SimpleAdmin branding
  - LoginModel PageModel using SignInManager.PasswordSignInAsync with ReturnUrl redirect

affects:
  - 01-04-smoke-test
  - 02-frontend

# Tech tracking
tech-stack:
  added: []
  patterns:
    - OpenIddictConstants.Claims.* for claim type constants (not OpenIddict.Abstractions.Claims.* which does not exist in v7.x)
    - using Microsoft.AspNetCore for GetOpenIddictServerRequest extension method (in Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers)
    - identity.SetClaim() extension method from OpenIddict.Abstractions for building ClaimsIdentity
    - Fully-qualified @model directive in .cshtml for Razor Pages in non-default namespace

key-files:
  created:
    - SimpleAdmin.Api/Controllers/AuthorizationController.cs
    - SimpleAdmin.Api/Controllers/ApiController.cs
    - SimpleAdmin.Api/Pages/Account/Login.cshtml
    - SimpleAdmin.Api/Pages/Account/Login.cshtml.cs
  modified: []

key-decisions:
  - "OpenIddictConstants.Claims.Subject/Email/Name are the correct claim constants in v7.x (not OpenIddict.Abstractions.Claims.* which does not exist)"
  - "GetOpenIddictServerRequest extension method is in Microsoft.AspNetCore namespace (class OpenIddictServerAspNetCoreHelpers)"
  - "Fully-qualified @model directive required in Login.cshtml: SimpleAdmin.Api.Pages.Account.LoginModel"

patterns-established:
  - "AuthorizationController pattern: authenticate cookie scheme, challenge if not authenticated, build ClaimsIdentity with SetClaim extensions, SignIn with OpenIddict server scheme"
  - "Login Razor page: standalone HTML document (no _Layout), embedded CSS only, asp-for tag helpers, PasswordSignInAsync + ReturnUrl redirect"

requirements-completed: [AUTH-01, AUTH-03]

# Metrics
duration: 8min
completed: 2026-03-11
---

# Phase 1 Plan 03: AuthorizationController, Login Page, and Protected API Endpoint Summary

**OpenIddict passthrough authorize/token controller, minimal Razor login page, and GET /api/me protected endpoint completing the Authorization Code + PKCE flow end-to-end**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-11T10:00:49Z
- **Completed:** 2026-03-11T10:08:00Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments

- Created AuthorizationController with GET+POST /connect/authorize (redirects to login when unauthenticated, builds ClaimsIdentity from cookie principal and signs in with OpenIddict server scheme) and POST /connect/token (exchanges auth code for token)
- Created ApiController with GET /api/me protected by `[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]` returning sub+email claims
- Created Login Razor page: minimal centered white card (360px), "SimpleAdmin" h1, email/password inputs, "Sign In" button, conditional red error banner — no framework CSS

## Task Commits

Each task was committed atomically:

1. **Task 1: Create AuthorizationController and ApiController** - `32b13b5` (feat)
2. **Task 2: Create Razor login page with minimal centered styling** - `ba154b2` (feat)

**Plan metadata:** (to be committed after SUMMARY)

## Files Created/Modified

- `SimpleAdmin.Api/Controllers/AuthorizationController.cs` - Passthrough authorize (GET+POST) and token exchange (POST) endpoints using OpenIddict server + cookie schemes
- `SimpleAdmin.Api/Controllers/ApiController.cs` - Protected GET /api/me returning sub and email claims from OpenIddict validation
- `SimpleAdmin.Api/Pages/Account/Login.cshtml` - Standalone Razor login page with embedded plain CSS, no framework dependencies
- `SimpleAdmin.Api/Pages/Account/Login.cshtml.cs` - LoginModel PageModel with OnGet and OnPostAsync using SignInManager.PasswordSignInAsync

## Decisions Made

- Used `OpenIddictConstants.Claims.*` constants (not `OpenIddict.Abstractions.Claims.*` which doesn't exist as a static class in v7.x)
- Used `using Microsoft.AspNetCore` to bring in the `GetOpenIddictServerRequest` extension method defined on `HttpContext`
- Used fully-qualified `@model SimpleAdmin.Api.Pages.Account.LoginModel` in the cshtml file since Razor Pages in non-default paths can't resolve the short name without a `_ViewImports.cshtml` file

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed Claims constants namespace — OpenIddictConstants.Claims.* not OpenIddict.Abstractions.Claims***
- **Found during:** Task 1 (AuthorizationController and ApiController)
- **Issue:** Plan specified `OpenIddict.Abstractions.Claims` but this static class does not exist in OpenIddict 7.3.0 — same pattern as Plan 02's `OpenIddictConstants` fix
- **Fix:** Used `OpenIddictConstants.Claims.Subject`, `OpenIddictConstants.Claims.Email`, `OpenIddictConstants.Claims.Name` throughout
- **Files modified:** AuthorizationController.cs, ApiController.cs
- **Verification:** `dotnet build` succeeds with 0 errors
- **Committed in:** 32b13b5 (Task 1 commit)

**2. [Rule 1 - Bug] Added `using Microsoft.AspNetCore` for GetOpenIddictServerRequest extension method**
- **Found during:** Task 1 (AuthorizationController compilation)
- **Issue:** `HttpContext.GetOpenIddictServerRequest()` extension method is defined in class `Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers` — the `Microsoft.AspNetCore` namespace using is required even though implicit usings provide many Microsoft.AspNetCore.* sub-namespaces
- **Fix:** Added `using Microsoft.AspNetCore;` to AuthorizationController.cs
- **Files modified:** AuthorizationController.cs
- **Verification:** `dotnet build` succeeds with 0 errors
- **Committed in:** 32b13b5 (Task 1 commit)

**3. [Rule 1 - Bug] Fully-qualified @model directive in Login.cshtml**
- **Found during:** Task 2 (Login page compilation)
- **Issue:** `@model LoginModel` fails because no `_ViewImports.cshtml` resolves the Pages.Account namespace for Razor compilation
- **Fix:** Changed to `@model SimpleAdmin.Api.Pages.Account.LoginModel`
- **Files modified:** SimpleAdmin.Api/Pages/Account/Login.cshtml
- **Verification:** `dotnet build` succeeds with 0 errors
- **Committed in:** ba154b2 (Task 2 commit)

---

**Total deviations:** 3 auto-fixed (3 bugs — all namespace/type resolution issues in OpenIddict 7.x)
**Impact on plan:** All auto-fixes necessary for compilation. No scope creep. The correct OpenIddict 7.x APIs behave identically to what the plan intended.

## Issues Encountered

- Pre-existing NU1900 warnings from private Azure DevOps NuGet feeds (iboris) — unrelated to this plan, present in all builds

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Full Authorization Code + PKCE flow is now wired: /connect/authorize -> /Account/Login -> POST login -> /connect/token -> GET /api/me
- GET /api/me without Bearer token will return 401
- GET /connect/authorize without cookie session will redirect to /Account/Login
- Admin user (admin@simpleadmin.local / Admin1234!) and SPA client seeded on startup by OpenIddictWorker
- Plan 01-04 (smoke test) can proceed immediately to verify the full flow end-to-end

---
*Phase: 01-backend-foundation*
*Completed: 2026-03-11*
