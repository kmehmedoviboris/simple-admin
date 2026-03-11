---
phase: 01-backend-foundation
plan: 02
subsystem: auth
tags: [dotnet, openiddict, oauth2, pkce, identity, cors, razorpages]

# Dependency graph
requires:
  - phase: 01-backend-foundation/01-01
    provides: ApplicationDbContext with UseOpenIddict(), AddIdentity, shared InMemoryDatabaseRoot
provides:
  - OpenIddict server configured for Authorization Code + PKCE with passthrough mode
  - OpenIddict validation using local server as default authentication scheme
  - CORS policy for http://localhost:5173
  - OpenIddictWorker seeding admin user (admin@simpleadmin.local) and SPA client (simple-admin-spa)
  - Razor Pages registration for login page in Plan 03
affects:
  - 01-03-auth-controller-login
  - 01-04-smoke-test

# Tech tracking
tech-stack:
  added:
    - OpenIddict.AspNetCore 7.3.0 (server + validation + ASP.NET Core integration)
    - Microsoft.AspNetCore.Authentication.Cookies (cookie scheme for login challenge)
  patterns:
    - OpenIddictValidationAspNetCoreDefaults as DefaultScheme (not JwtBearer, not Cookie)
    - IHostedService pattern for startup seeding (OpenIddictWorker)
    - Passthrough mode for authorize and token endpoints (controller handles them in Plan 03)
    - OpenIddictConstants.ClientTypes / OpenIddictConstants.Permissions for v7.x constants

key-files:
  created:
    - SimpleAdmin.Api/Workers/OpenIddictWorker.cs
  modified:
    - SimpleAdmin.Api/Program.cs

key-decisions:
  - "Use OpenIddictConstants.ClientTypes and OpenIddictConstants.Permissions — not OpenIddict.Abstractions.ClientTypes — in OpenIddict 7.3.0"
  - "DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme"
  - "DisableAccessTokenEncryption() for POC simplicity"

patterns-established:
  - "OpenIddict seeder pattern: IHostedService + CreateAsyncScope + EnsureCreatedAsync before seeding"
  - "Middleware order: UseRouting > UseCors > UseAuthentication > UseAuthorization > MapControllers > MapRazorPages"

requirements-completed: [AUTH-01, AUTH-03]

# Metrics
duration: 4min
completed: 2026-03-11
---

# Phase 1 Plan 02: OpenIddict Server Configuration and SPA Client Seeder Summary

**OpenIddict Authorization Code + PKCE server with passthrough endpoints, OpenIddictValidation as default scheme, CORS for localhost:5173, and IHostedService seeding admin user and SPA client on startup**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-11T09:53:42Z
- **Completed:** 2026-03-11T09:57:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Configured OpenIddict server with Authorization Code + PKCE, passthrough mode on authorize/token endpoints, dev certs, disabled access token encryption
- Set authentication DefaultScheme to OpenIddictValidationAspNetCoreDefaults (not JwtBearer) per locked project decision
- Created OpenIddictWorker IHostedService that seeds admin user (admin@simpleadmin.local / Admin1234!) and SPA client (simple-admin-spa) on every startup

## Task Commits

Each task was committed atomically:

1. **Task 1: Configure OpenIddict server, validation, authentication, CORS, Razor Pages** - `225207e` (feat)
2. **Task 2: Create OpenIddictWorker hosted seeder** - `61f941e` (feat)

**Plan metadata:** (to be committed after SUMMARY)

## Files Created/Modified

- `SimpleAdmin.Api/Program.cs` - Full OpenIddict server + validation + authentication registration, CORS, Razor Pages, correct middleware order
- `SimpleAdmin.Api/Workers/OpenIddictWorker.cs` - IHostedService seeding admin user and simple-admin-spa client with AuthorizationCode + PKCE permissions

## Decisions Made

- Used `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme` as DefaultScheme per locked project decision
- `DisableAccessTokenEncryption()` for POC simplicity (tokens readable without decryption key)
- Cookie scheme as DefaultChallengeScheme only — redirects unauthenticated users to /Account/Login

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed OpenIddictConstants class name for ClientTypes and Permissions**
- **Found during:** Task 2 (OpenIddictWorker compilation)
- **Issue:** Plan used `OpenIddict.Abstractions.ClientTypes.Public` and `OpenIddict.Abstractions.Permissions.*` — these types do not exist as top-level classes in the `OpenIddict.Abstractions` namespace in v7.3.0
- **Fix:** Changed to `OpenIddictConstants.ClientTypes.Public` and `OpenIddictConstants.Permissions.*` — the static constants class that actually contains these values in v7.x
- **Files modified:** SimpleAdmin.Api/Workers/OpenIddictWorker.cs
- **Verification:** `dotnet build` succeeds with 0 errors
- **Committed in:** 61f941e (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 bug — wrong static class name for OpenIddict constants in v7.3.0)
**Impact on plan:** Auto-fix was necessary for compilation. No scope creep. The constants access the same string values.

## Issues Encountered

- Private Azure DevOps NuGet feeds (iboris) in user's NuGet config return 401 Unauthorized causing NU1900 warnings on every build. Pre-existing environment issue, not caused by this plan.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- OpenIddict server fully configured; authorize and token endpoints in passthrough mode awaiting Plan 03 controller
- SPA client (simple-admin-spa) and admin user seeded on startup
- CORS configured for http://localhost:5173 (Vue SPA origin)
- Razor Pages registered for the login page in Plan 03
- Plan 01-03 (auth controller + login page) can proceed immediately
- No blockers

---
*Phase: 01-backend-foundation*
*Completed: 2026-03-11*

## Self-Check: PASSED

All files verified present. All commits verified in git log.
