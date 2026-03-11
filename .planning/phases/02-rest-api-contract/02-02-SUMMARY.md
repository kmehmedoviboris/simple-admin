---
phase: 02-rest-api-contract
plan: 02
subsystem: testing
tags: [xunit, aspnet-core, integration-tests, openiddict, pkce, identity, openapi]

# Dependency graph
requires:
  - phase: 02-rest-api-contract
    plan: 01
    provides: "UsersController CRUD endpoints, DTOs (UserListDto/CreateUserDto/UpdateUserDto), 3 seeded users, OpenAPI spec at /openapi/v1.json"
  - phase: 01-backend-foundation
    provides: "ApplicationUser, OpenIddict PKCE auth stack, TestWebApplicationFactory, AuthFlowSmokeTests PKCE pattern"
provides:
  - "TokenHelper.GetAccessTokenAsync: reusable PKCE token acquisition for all integration test classes"
  - "UsersControllerTests: 10 integration tests proving USER-01 through USER-04 + 401 auth + OpenAPI spec"
affects: [03-spa-client, 04-e2e-tests]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Static TokenHelper pattern: extract PKCE flow into shared helper so test classes don't duplicate auth logic"
    - "Unique-per-test email via Guid.NewGuid() prevents cross-test interference on shared in-memory DB"
    - "CreateAuthenticatedClientAsync helper method: single-line auth setup per test class"

key-files:
  created:
    - "SimpleAdmin.Tests/Helpers/TokenHelper.cs"
    - "SimpleAdmin.Tests/UsersControllerTests.cs"
  modified: []

key-decisions:
  - "TokenHelper receives HttpClient as parameter — caller controls AllowAutoRedirect=false + HandleCookies=true configuration"
  - "Use Guid.NewGuid() in test email addresses to prevent cross-test DB collisions on shared in-memory database"
  - "AuthFlowSmokeTests left untouched — existing passing tests not refactored to use TokenHelper (out of scope)"

patterns-established:
  - "Integration test pattern: IClassFixture<TestWebApplicationFactory> + CreateAuthenticatedClientAsync helper"
  - "Test isolation: unique emails per test (create-test-{guid}@test.local format)"
  - "TokenHelper.GetAccessTokenAsync as the standard way to acquire Bearer tokens in any future test class"

requirements-completed: [USER-01, USER-02, USER-03, USER-04]

# Metrics
duration: 7min
completed: 2026-03-11
---

# Phase 2 Plan 02: Integration Tests for UsersController Summary

**10 xUnit integration tests proving full CRUD + 401 auth + OpenAPI spec via PKCE TokenHelper for admin Bearer token acquisition**

## Performance

- **Duration:** 7 min
- **Started:** 2026-03-11T12:21:51Z
- **Completed:** 2026-03-11T12:28:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- Created `TokenHelper.GetAccessTokenAsync` encapsulating the full PKCE auth code flow (extracted from AuthFlowSmokeTests) as a reusable static method for all future integration test classes
- Created `UsersControllerTests` with 10 passing tests covering all USER-01 through USER-04 scenarios including edge cases (duplicate email 400, nonexistent id 404s, absence-after-delete confirmation)
- All 13 tests pass (3 Phase 1 + 10 Phase 2) with zero regressions

## Task Commits

Each task was committed atomically:

1. **Task 1: Extract TokenHelper from AuthFlowSmokeTests for reusable token acquisition** - `9f130a8` (feat)
2. **Task 2: Create UsersControllerTests covering all CRUD operations and auth** - `8507abc` (feat)

**Plan metadata:** (docs commit — see below)

## Files Created/Modified
- `SimpleAdmin.Tests/Helpers/TokenHelper.cs` - Static `GetAccessTokenAsync(client, email, password)` that drives PKCE flow: authorize → login page → form POST → redirect chain → token exchange, throws `InvalidOperationException` on any failure
- `SimpleAdmin.Tests/UsersControllerTests.cs` - 10 Fact tests: GetAll (USER-01), Create valid + duplicate (USER-02), Update email + password + 404 (USER-03), Delete + confirm absence + 404 (USER-04), 401 without token, OpenAPI spec path

## Decisions Made
- `TokenHelper` receives `HttpClient` as a parameter — the caller creates the client with `AllowAutoRedirect = false, HandleCookies = true` from `TestWebApplicationFactory`; TokenHelper does not create or configure the client
- Each test that creates users uses `Guid.NewGuid()` in the email to guarantee uniqueness across parallel/sequential test runs sharing the same in-memory database
- `AuthFlowSmokeTests` was deliberately not refactored to use TokenHelper — changing existing passing tests is out of scope for this plan

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- NU1900 warnings from private Azure DevOps NuGet feeds (Telerik, Unicorn, iTOS) appear during test runs — these are expected (not authenticated to corporate feeds), they are warnings only and do not affect build or test execution.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All USER-01 through USER-04 requirements have automated passing tests
- TokenHelper is ready for reuse in any future test class (e.g., Phase 4 E2E tests)
- Phase 3 (SPA client) can proceed with confidence that the /api/users REST surface is fully verified
- OpenAPI spec endpoint confirmed working and accessible for `@hey-api/openapi-ts` code generation

---
*Phase: 02-rest-api-contract*
*Completed: 2026-03-11*
