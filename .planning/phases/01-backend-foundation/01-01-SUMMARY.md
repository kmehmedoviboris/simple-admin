---
phase: 01-backend-foundation
plan: 01
subsystem: database
tags: [dotnet, efcore, openiddict, identity, inmemory]

# Dependency graph
requires: []
provides:
  - .NET 10 webapi project with OpenIddict 7.3.0 + EF Core in-memory setup
  - ApplicationUser extending IdentityUser
  - ApplicationDbContext extending IdentityDbContext<ApplicationUser> with UseOpenIddict()
  - Shared InMemoryDatabaseRoot singleton registered in DI
  - AddIdentity<ApplicationUser, IdentityRole> (not AddDefaultIdentity)
affects:
  - 01-02-openiddict-server-config
  - 01-03-auth-controller-login
  - 01-04-smoke-test

# Tech tracking
tech-stack:
  added:
    - OpenIddict.AspNetCore 7.3.0
    - OpenIddict.EntityFrameworkCore 7.3.0
    - Microsoft.EntityFrameworkCore.InMemory 10.0.4
    - Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.4
  patterns:
    - Shared InMemoryDatabaseRoot singleton passed to UseInMemoryDatabase to avoid data isolation across DbContext instances
    - AddIdentity (not AddDefaultIdentity) to avoid default UI conflicts with OpenIddict

key-files:
  created:
    - SimpleAdmin.Api/SimpleAdmin.Api.csproj
    - SimpleAdmin.slnx
    - SimpleAdmin.Api/Models/ApplicationUser.cs
    - SimpleAdmin.Api/Data/ApplicationDbContext.cs
  modified:
    - SimpleAdmin.Api/Program.cs

key-decisions:
  - "Use AddIdentity<ApplicationUser, IdentityRole> (not AddDefaultIdentity) to avoid default UI conflicts with OpenIddict"
  - "Pass shared InMemoryDatabaseRoot singleton to UseInMemoryDatabase() so all DbContext instances share the same in-memory data"
  - "Added Microsoft.AspNetCore.Identity.EntityFrameworkCore explicitly — not pulled transitively by OpenIddict"

patterns-established:
  - "Shared InMemoryDatabaseRoot: register as singleton and inject via sp.GetRequiredService<InMemoryDatabaseRoot>() in DbContext options"
  - "Identity registration: always AddIdentity<ApplicationUser, IdentityRole> in this project"

requirements-completed: [AUTH-01, AUTH-03]

# Metrics
duration: 4min
completed: 2026-03-11
---

# Phase 1 Plan 01: EF Core In-Memory DbContext with Identity + OpenIddict Tables Summary

**.NET 10 webapi scaffold with OpenIddict 7.3.0, EF Core in-memory database, shared InMemoryDatabaseRoot, and IdentityDbContext wired with UseOpenIddict()**

## Performance

- **Duration:** 4 min
- **Started:** 2026-03-11T09:45:11Z
- **Completed:** 2026-03-11T09:49:00Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments

- Created .NET 10 webapi project + SimpleAdmin.slnx solution file with all required NuGet packages
- Implemented ApplicationUser (extends IdentityUser) and ApplicationDbContext (extends IdentityDbContext with UseOpenIddict())
- Configured Program.cs with shared InMemoryDatabaseRoot singleton, AddIdentity (not AddDefaultIdentity), and minimal controller pipeline

## Task Commits

Each task was committed atomically:

1. **Task 1: Create .NET 10 project with NuGet packages and solution file** - `47161da` (chore)
2. **Task 2: Create ApplicationUser, ApplicationDbContext, register EF Core** - `199b130` (feat)

**Plan metadata:** (to be committed after SUMMARY)

## Files Created/Modified

- `SimpleAdmin.slnx` - Solution file referencing SimpleAdmin.Api project
- `SimpleAdmin.Api/SimpleAdmin.Api.csproj` - Project file with OpenIddict 7.3.0, EF Core InMemory 10.0.4, Identity.EF 10.0.4
- `SimpleAdmin.Api/Models/ApplicationUser.cs` - ApplicationUser extending IdentityUser
- `SimpleAdmin.Api/Data/ApplicationDbContext.cs` - IdentityDbContext<ApplicationUser> with UseOpenIddict() via builder config
- `SimpleAdmin.Api/Program.cs` - DI registration: InMemoryDatabaseRoot, DbContext with UseOpenIddict, AddIdentity, AddControllers
- `.gitignore` - Standard .NET gitignore

## Decisions Made

- Used `AddIdentity<ApplicationUser, IdentityRole>` (not `AddDefaultIdentity`) per locked project decision — avoids default UI conflicts with OpenIddict
- Registered `InMemoryDatabaseRoot` as a singleton and passed to `UseInMemoryDatabase()` per locked project decision — ensures data sharing across DbContext instances
- Added `Microsoft.AspNetCore.Identity.EntityFrameworkCore` explicitly as a direct dependency since OpenIddict does not pull it transitively

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added Microsoft.AspNetCore.Identity.EntityFrameworkCore package**
- **Found during:** Task 2 (ApplicationDbContext creation)
- **Issue:** `IdentityDbContext<ApplicationUser>` requires `Microsoft.AspNetCore.Identity.EntityFrameworkCore` package, which OpenIddict does not pull as a transitive dependency
- **Fix:** Added `dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --source https://api.nuget.org/v3/index.json`
- **Files modified:** SimpleAdmin.Api/SimpleAdmin.Api.csproj
- **Verification:** Build succeeds without errors
- **Committed in:** 199b130 (Task 2 commit)

**2. [Rule 1 - Bug] Fixed InMemoryDatabaseRoot namespace**
- **Found during:** Task 2 (Program.cs compilation)
- **Issue:** Plan suggested `Microsoft.EntityFrameworkCore.Infrastructure.Memory` namespace — does not exist in EF Core 10; correct namespace is `Microsoft.EntityFrameworkCore.Storage`
- **Fix:** Changed using directive to `using Microsoft.EntityFrameworkCore.Storage;`
- **Files modified:** SimpleAdmin.Api/Program.cs
- **Verification:** Build succeeds, `InMemoryDatabaseRoot` resolves correctly
- **Committed in:** 199b130 (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (1 missing critical package, 1 namespace bug)
**Impact on plan:** Both auto-fixes were necessary for correctness. No scope creep.

## Issues Encountered

- Private Azure DevOps NuGet feeds (iboris) in user's NuGet config return 401 Unauthorized causing NU1900 warnings on every build. Used `--source https://api.nuget.org/v3/index.json` flag when adding packages. Warnings are pre-existing environment issue, not caused by this plan.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Project compiles cleanly (0 errors)
- InMemoryDatabaseRoot, IdentityDbContext, UseOpenIddict, and AddIdentity are all in place
- Plan 01-02 (OpenIddict server configuration) can proceed immediately
- No blockers

---
*Phase: 01-backend-foundation*
*Completed: 2026-03-11*

## Self-Check: PASSED

All files verified present. All commits verified in git log.
