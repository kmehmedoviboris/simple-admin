---
phase: 03-vue-spa-oidc
plan: 02
subsystem: auth
tags: [vue3, typescript, hey-api, openapi-ts, playwright, oidc, bearer-token, e2e-tests]

requires:
  - phase: 03-vue-spa-oidc-01
    provides: Pinia authStore with accessToken getter, Vue Router with /users route, oidcManager singleton
  - phase: 02-rest-api-contract
    provides: GET /openapi/v1.json endpoint, /api/users protected routes

provides:
  - Generated typed API client in simple-admin-spa/src/client/ from backend OpenAPI spec
  - Bearer token request interceptor injecting Authorization header on all API calls
  - 401 response interceptor clearing tokens and triggering login redirect
  - Playwright e2e smoke tests proving full AUTH-02 login/logout flow
  - playwright.config.ts with webServer config for backend + SPA

affects: [04-user-crud-views]

tech-stack:
  added:
    - "@hey-api/openapi-ts 0.93.1 (typed API client generation from OpenAPI spec)"
    - "@hey-api/client-fetch 0.13.1 (fetch-based HTTP client used by generated code)"
    - "@playwright/test 1.58.2 (e2e test framework)"
    - "Chromium browser (via npx playwright install chromium)"
  patterns:
    - "hey-api client.interceptors.request.use for Bearer token injection (Request object API)"
    - "hey-api client.interceptors.response.use for 401 handling"
    - "Generated src/client/ imported via @/client/client.gen in main.ts"
    - "Playwright loginAsAdmin() helper reused across auth flow tests"
    - "sessionStorage scan in page.evaluate to retrieve access_token for api bearer test"

key-files:
  created:
    - simple-admin-spa/openapi-ts.config.ts
    - simple-admin-spa/src/client/client.gen.ts
    - simple-admin-spa/src/client/sdk.gen.ts
    - simple-admin-spa/src/client/types.gen.ts
    - simple-admin-spa/src/client/index.ts
    - simple-admin-spa/src/client/client/
    - simple-admin-spa/src/client/core/
    - simple-admin-spa/playwright.config.ts
    - simple-admin-spa/tests/auth.spec.ts
  modified:
    - simple-admin-spa/src/main.ts
    - simple-admin-spa/src/auth/oidcManager.ts
    - simple-admin-spa/package.json
    - SimpleAdmin.Api/Workers/OpenIddictWorker.cs

key-decisions:
  - "hey-api generated client already embeds baseUrl from the OpenAPI spec server URL — no explicit setConfig({baseUrl}) call needed in main.ts"
  - "Backend runs on port 5009 (launchSettings.json) not 5000 — all SPA config updated: oidcManager authority, openapi-ts.config.ts input URL, playwright.config.ts webServer URL"
  - "OpenIddict v7 requires 'api' custom scope to be registered via IOpenIddictScopeManager.CreateAsync, not just granted as a permission on the client application"
  - "Playwright api bearer test reads access_token directly from sessionStorage (oidc-client-ts stores user object under oidc.user: prefix) and calls /api/users with Bearer header"

patterns-established:
  - "Pattern: hey-api request interceptor — client.interceptors.request.use receives Web API Request object (not options object); use request.headers.set() not request.headers['name']"
  - "Pattern: Playwright e2e for OIDC — navigate to protected route, waitForURL('**/Account/Login**'), fill credentials, submit, waitForURL('**/users')"
  - "Pattern: OpenIddict scope registration — all scopes requested by clients must be registered via IOpenIddictScopeManager (not just listed as permissions on the application)"

requirements-completed: [AUTH-02]

duration: 13min
completed: 2026-03-11
---

# Phase 3 Plan 02: API Client and E2E Tests Summary

**hey-api typed API client generated from backend OpenAPI spec with Bearer interceptor and 401 redirect, plus 6 Playwright smoke tests proving full OIDC login/logout flow end-to-end**

## Performance

- **Duration:** 13 min
- **Started:** 2026-03-11T13:11:41Z
- **Completed:** 2026-03-11T13:24:21Z
- **Tasks:** 2
- **Files modified:** 13 (9 SPA created, 3 SPA modified, 1 backend modified)

## Accomplishments

- hey-api typed API client generated in `src/client/` with full TypeScript types for all `/api/users` CRUD endpoints and OIDC endpoints
- Bearer token request interceptor automatically injects `Authorization: Bearer <token>` header on every API call using the Pinia authStore's `accessToken` getter
- 401 response interceptor clears user state and triggers `signinRedirect()` — no intermediate screen, immediate login redirect
- All 6 Playwright smoke tests pass (16s total): unauthenticated redirect, login flow, session persistence, logout, protected route after logout, api bearer header verification

## Task Commits

1. **Task 1: Generate typed API client and wire Bearer interceptor with 401 handling** - `be4bc43` (feat)
2. **Task 2: Playwright e2e smoke tests for OIDC login/logout flow** - `88b149c` (feat)

## Files Created/Modified

**SPA (created):**
- `simple-admin-spa/openapi-ts.config.ts` - hey-api config pointing at backend OpenAPI spec
- `simple-admin-spa/src/client/` - Generated typed API client (client.gen.ts, sdk.gen.ts, types.gen.ts, index.ts, client/, core/)
- `simple-admin-spa/playwright.config.ts` - Playwright config with webServer for backend (5009) + SPA (5173)
- `simple-admin-spa/tests/auth.spec.ts` - 6 AUTH-02 smoke tests with loginAsAdmin() helper

**SPA (modified):**
- `simple-admin-spa/src/main.ts` - Added client interceptors for Bearer injection and 401 handling
- `simple-admin-spa/src/auth/oidcManager.ts` - Fixed authority port from 5000 to 5009
- `simple-admin-spa/package.json` - Added @hey-api/openapi-ts, @hey-api/client-fetch, @playwright/test; added openapi-ts script

**Backend (modified):**
- `SimpleAdmin.Api/Workers/OpenIddictWorker.cs` - Added 'api' scope registration to fix OpenIddict v7 scope validation

## Decisions Made

- **hey-api baseUrl not needed in setConfig:** The generated `client.gen.ts` already embeds `baseUrl: 'http://localhost:5009/'` from the OpenAPI spec's `servers` field. No explicit `client.setConfig()` call needed.
- **Port 5009 throughout:** The launchSettings.json configures the backend on port 5009. Updated `oidcManager.ts` (authority), `openapi-ts.config.ts` (input URL), and `playwright.config.ts` (webServer URL) to match.
- **No setConfig call:** Since baseUrl is already baked into client.gen.ts by the generator, the plan's `client.setConfig({ baseUrl: 'http://localhost:5000' })` step was skipped — the generated URL is already correct.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed all references from port 5000 to port 5009**
- **Found during:** Task 1 (Generate typed API client)
- **Issue:** Plan specifies port 5000 throughout. The actual backend runs on port 5009 (launchSettings.json). `oidcManager.ts` authority was `http://localhost:5000`, meaning the SPA could never authenticate. The curl test of `/connect/authorize` confirmed the backend is not accessible on port 5000.
- **Fix:** Updated `oidcManager.ts` authority to `http://localhost:5009`, `openapi-ts.config.ts` input URL to `http://localhost:5009/openapi/v1.json`, and `playwright.config.ts` webServer URL to `http://localhost:5009/.well-known/openid-configuration`
- **Files modified:** simple-admin-spa/src/auth/oidcManager.ts, simple-admin-spa/openapi-ts.config.ts, simple-admin-spa/playwright.config.ts
- **Verification:** `openapi-ts` generated client successfully; curl of authorize endpoint returned 302 to /Account/Login
- **Committed in:** be4bc43 (Task 1 commit)

**2. [Rule 1 - Bug] Registered 'api' scope in OpenIddictWorker to fix 400 invalid_scope error**
- **Found during:** Task 2 (Playwright tests — all 6 tests failing on OIDC redirect)
- **Issue:** The OIDC authorize endpoint returned HTTP 400 `error:invalid_scope` when the SPA requested `scope=openid email profile api`. OpenIddict v7 validates that all requested scopes are registered via `IOpenIddictScopeManager`. The 'api' scope was granted as a permission on the application but never registered as a scope entity.
- **Fix:** Added `"api"` to the scope registration loop in `OpenIddictWorker.cs` so it's registered via `IOpenIddictScopeManager.CreateAsync` alongside email and profile.
- **Files modified:** SimpleAdmin.Api/Workers/OpenIddictWorker.cs
- **Verification:** `curl /connect/authorize?...scope=openid email profile api...` returns HTTP 302 to /Account/Login after fix. All 6 Playwright tests pass.
- **Committed in:** 88b149c (Task 2 commit)

---

**Total deviations:** 2 auto-fixed (2 Rule 1 bugs)
**Impact on plan:** Both fixes were essential for the system to function. Port mismatch would prevent SPA from authenticating against the backend at all. Missing scope registration caused 400 errors on every OIDC authorization request. No scope creep.

## Issues Encountered

- The plan's "Start backend in background, generate, stop" pattern needed adjustment because the backend had already been started from a previous session on port 5009.
- Playwright's `waitForURL('**/Account/Login**')` initially timed out at the `/connect/authorize` URL — root cause was the scope validation 400 error preventing the authorize endpoint from returning its 302 redirect.

## User Setup Required

None - no external service configuration required. Tests run against localhost:5009 (backend) and localhost:5173 (SPA dev server) with Playwright's webServer auto-start.

## Next Phase Readiness

- Typed API client in `src/client/` is ready for use in Phase 4 user CRUD views via `getApiUsers`, `postApiUsers`, `putApiUsersById`, `deleteApiUsersById` from `@/client`
- Bearer interceptor is live — all `hey-api` calls automatically include the Authorization header
- 401 interceptor handles token expiry gracefully
- All AUTH-02 success criteria verified by automated Playwright tests

---
*Phase: 03-vue-spa-oidc*
*Completed: 2026-03-11*
