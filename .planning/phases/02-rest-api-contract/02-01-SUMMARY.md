---
phase: 02-rest-api-contract
plan: 01
subsystem: api
tags: [aspnet-core, openiddict, identity, openapi, scalar, crud, dtos]

# Dependency graph
requires:
  - phase: 01-backend-foundation
    provides: "ApplicationUser model, OpenIddict auth stack, OpenIddictWorker seeder, ApplicationDbContext"
provides:
  - "GET/POST/PUT/DELETE /api/users endpoints protected by OpenIddict Bearer tokens"
  - "UserListDto, CreateUserDto, UpdateUserDto records in SimpleAdmin.Api.Dtos"
  - "OpenAPI spec at /openapi/v1.json (development)"
  - "Scalar UI at /scalar (development)"
  - "3 seeded users: admin, alice, bob"
affects: [03-spa-client, 04-e2e-tests]

# Tech tracking
tech-stack:
  added:
    - "Microsoft.AspNetCore.OpenApi 10.0.4"
    - "Scalar.AspNetCore 2.13.5"
  patterns:
    - "DTO records as thin request/response types (no classes needed)"
    - "Synchronous .ToList() on UserManager.Users (no async equivalent available)"
    - "RemovePasswordAsync + AddPasswordAsync for password updates (not ChangePasswordAsync)"
    - "SetEmailAsync + SetUserNameAsync together to keep email and username in sync"

key-files:
  created:
    - "SimpleAdmin.Api/Dtos/UserListDto.cs"
    - "SimpleAdmin.Api/Dtos/CreateUserDto.cs"
    - "SimpleAdmin.Api/Dtos/UpdateUserDto.cs"
    - "SimpleAdmin.Api/Controllers/UsersController.cs"
  modified:
    - "SimpleAdmin.Api/Program.cs"
    - "SimpleAdmin.Api/Workers/OpenIddictWorker.cs"
    - "SimpleAdmin.Api/SimpleAdmin.Api.csproj"

key-decisions:
  - "Use synchronous .ToList() on UserManager.Users — no async equivalent is available for this LINQ projection"
  - "RemovePasswordAsync + AddPasswordAsync for password change — ChangePasswordAsync requires current password which admin updates don't have"
  - "OpenAPI and Scalar registered under IsDevelopment() guard — not exposed in production"
  - "SetEmailAsync + SetUserNameAsync called together to keep UserName and Email in sync on update"

patterns-established:
  - "DTO pattern: C# record types in SimpleAdmin.Api.Dtos namespace for all request/response types"
  - "Auth pattern: [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)] on all user-facing controllers"
  - "ProducesResponseType attributes on all actions for complete OpenAPI schema generation"

requirements-completed: [USER-01, USER-02, USER-03, USER-04]

# Metrics
duration: 8min
completed: 2026-03-11
---

# Phase 2 Plan 01: REST API Contract Summary

**Full CRUD REST API for user management at /api/users with OpenAPI spec at /openapi/v1.json and 3 seeded users via Scalar.AspNetCore + Microsoft.AspNetCore.OpenApi**

## Performance

- **Duration:** 8 min
- **Started:** 2026-03-11T11:14:14Z
- **Completed:** 2026-03-11T11:22:00Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments
- Created UsersController with GET/POST/PUT/DELETE at /api/users, all protected by OpenIddict Bearer token (401 without token)
- Added 3 DTO records (UserListDto, CreateUserDto, UpdateUserDto) in new Dtos/ folder with proper ProducesResponseType attributes
- Wired OpenAPI spec generation (AddOpenApi + MapOpenApi) and Scalar dev UI (MapScalarApiReference) under development guard
- Extended OpenIddictWorker to seed alice@simpleadmin.local and bob@simpleadmin.local alongside admin (3 total seeded users)
- All 3 existing Phase 1 smoke tests continue to pass (no regressions)

## Task Commits

Each task was committed atomically:

1. **Task 1: Create DTO records and UsersController with CRUD endpoints** - `8b14cdd` (feat)
2. **Task 2: Add OpenAPI + Scalar packages, register in Program.cs, seed test users** - `47081b6` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified
- `SimpleAdmin.Api/Dtos/UserListDto.cs` - Response DTO: `record UserListDto(string Id, string Email, bool EmailConfirmed)`
- `SimpleAdmin.Api/Dtos/CreateUserDto.cs` - Request DTO: `record CreateUserDto(string Email, string Password)`
- `SimpleAdmin.Api/Dtos/UpdateUserDto.cs` - Request DTO: `record UpdateUserDto(string? Email, string? NewPassword)` (both nullable for partial updates)
- `SimpleAdmin.Api/Controllers/UsersController.cs` - CRUD controller with GET/POST/PUT/DELETE, OpenIddict Bearer auth, ProducesResponseType on all actions
- `SimpleAdmin.Api/Program.cs` - Added AddOpenApi(), MapOpenApi(), MapScalarApiReference(), Scalar.AspNetCore using
- `SimpleAdmin.Api/Workers/OpenIddictWorker.cs` - Added alice + bob test user seeding after admin seed block
- `SimpleAdmin.Api/SimpleAdmin.Api.csproj` - Added Microsoft.AspNetCore.OpenApi 10.0.4 and Scalar.AspNetCore 2.13.5

## Decisions Made
- Used synchronous `.ToList()` on `UserManager.Users` — there is no `ToListAsync` equivalent for LINQ projections over IdentityUser
- Used `RemovePasswordAsync` + `AddPasswordAsync` for password updates rather than `ChangePasswordAsync` — admin operations don't require the current password
- OpenAPI and Scalar endpoints registered only under `IsDevelopment()` to avoid exposing them in production
- `SetEmailAsync` and `SetUserNameAsync` called together on email update to keep `UserName` in sync with `Email` (they are separate Identity properties that must match)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Private Azure DevOps NuGet feeds (Telerik, Unicorn, iTOS) returned 401 errors during `dotnet add package`. This is expected (not authenticated to corporate feeds). Packages were fetched successfully from nuget.org. Build passes cleanly when using `--source https://api.nuget.org/v3/index.json`.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Full /api/users REST surface is live with correct auth behavior
- OpenAPI spec at /openapi/v1.json is available in development for Phase 3 client code generation
- 3 seeded users (admin, alice, bob) provide meaningful data for Phase 3 SPA integration
- Phase 3 can use `@hey-api/openapi-ts` to generate a typed client from /openapi/v1.json

---
*Phase: 02-rest-api-contract*
*Completed: 2026-03-11*

## Self-Check: PASSED

- All 7 key files verified present on disk
- Task commits 8b14cdd and 47081b6 verified in git log
- dotnet build: 0 errors
- dotnet test: 3/3 Phase 1 tests pass (no regressions)
