---
phase: 03-vue-spa-oidc
plan: 01
subsystem: auth
tags: [vue3, vite, typescript, oidc-client-ts, pinia, vue-router, primevue, openiddict, pkce]

requires:
  - phase: 02-rest-api-contract
    provides: OpenIddict backend with /connect/authorize and /connect/token endpoints

provides:
  - OpenIddict end-session endpoint at /connect/logout (backend)
  - Vue 3 SPA project scaffolded at simple-admin-spa/
  - oidcManager singleton with PKCE Authorization Code flow config
  - Pinia authStore with initialize/login/logout/setUser actions
  - Vue Router with public /callback and /oidc-error routes; protected /users route
  - beforeEach auth guard redirecting unauthenticated users to signinRedirect
  - CallbackView handling signinRedirectCallback
  - AppShell with PrimeVue Toolbar and Logout button

affects: [03-vue-spa-oidc, 04-user-crud-views]

tech-stack:
  added:
    - oidc-client-ts 3.4.1 (PKCE OIDC in browser)
    - primevue 4.5.4 + @primeuix/themes 2.0.3 + primeicons 7.0.0 (UI components)
    - pinia 3.0.4 (auth state store)
    - vue-router 5.0.3 (SPA routing)
    - vite 7.3.1 + vue 3.5.29 + typescript 5.9.3 (build and language)
  patterns:
    - UserManager singleton exported from auth/oidcManager.ts (single sessionStorage store)
    - Pinia store wrapping oidc-client-ts (components never import userManager directly)
    - useAuthStore() called inside beforeEach body (not at module top level — Pinia not yet installed at import time)
    - Public route meta pattern (meta: { public: true }) for callback and error routes
    - app.use(pinia) before app.use(router) — critical ordering for guard access to stores
    - Dual SignOut pattern in backend: Identity ApplicationScheme + OpenIddict server scheme

key-files:
  created:
    - simple-admin-spa/src/auth/oidcManager.ts
    - simple-admin-spa/src/stores/authStore.ts
    - simple-admin-spa/src/router/index.ts
    - simple-admin-spa/src/views/CallbackView.vue
    - simple-admin-spa/src/views/OidcErrorView.vue
    - simple-admin-spa/src/views/UsersView.vue
    - simple-admin-spa/src/components/AppShell.vue
    - simple-admin-spa/src/App.vue
    - simple-admin-spa/src/main.ts
  modified:
    - SimpleAdmin.Api/Program.cs
    - SimpleAdmin.Api/Workers/OpenIddictWorker.cs
    - SimpleAdmin.Api/Controllers/AuthorizationController.cs

key-decisions:
  - "In OpenIddict v7, the correct method is SetEndSessionEndpointUris (not SetLogoutEndpointUris) and EnableEndSessionEndpointPassthrough (not EnableLogoutEndpointPassthrough)"
  - "In OpenIddict v7, the correct permission constant is OpenIddictConstants.Permissions.Endpoints.EndSession (not .Logout — the Logout constant does not exist in v7)"
  - "useAuthStore() is imported at module top level in router/index.ts but called inside beforeEach body — this is safe because the guard runs after app.use(pinia) completes"
  - "app.use(pinia) MUST come before app.use(router) to ensure Pinia stores are available in router navigation guards"
  - "SessionStorage is the userStore for oidc-client-ts (POC-appropriate; tab-scoped is accepted behavior)"

patterns-established:
  - "Pattern: UserManager singleton — single export from auth/oidcManager.ts avoids state conflicts"
  - "Pattern: beforeEach auth guard — calls initialize() if user is null, then login() if not authenticated"
  - "Pattern: Public route meta — /callback and /oidc-error marked meta: { public: true } to skip auth guard"
  - "Pattern: AppShell conditional — shown only when authenticated on non-public routes"

requirements-completed: [AUTH-02]

duration: 6min
completed: 2026-03-11
---

# Phase 3 Plan 01: Vue SPA OIDC Foundation Summary

**OpenIddict end-session endpoint added to .NET backend, full Vue 3 SPA scaffolded with oidc-client-ts PKCE flow, Pinia auth store, Vue Router auth guard, PrimeVue app shell, and callback/error views — SPA builds with 0 errors**

## Performance

- **Duration:** 6 min
- **Started:** 2026-03-11T12:42:00Z
- **Completed:** 2026-03-11T12:48:00Z
- **Tasks:** 2
- **Files modified:** 22 (3 backend, 19 SPA)

## Accomplishments

- Backend now exposes `/connect/logout` end-session endpoint and advertises `end_session_endpoint` in the OpenID Connect discovery document
- Vue 3 SPA at `simple-admin-spa/` with complete OIDC Authorization Code + PKCE flow via oidc-client-ts
- Auth guard protects `/users`; unauthenticated users are redirected to Razor login via `signinRedirect()`
- Callback route processes authorization code and sets user in Pinia store
- PrimeVue Toolbar app shell with Logout button triggering `signoutRedirect()` through the backend end-session endpoint

## Task Commits

1. **Task 1: Add OpenIddict logout endpoint to backend** - `53a10bf` (feat)
2. **Task 2: Scaffold Vue 3 SPA with OIDC auth, router guard, and app shell** - `20b14b7` (feat)

## Files Created/Modified

**Backend (modified):**
- `SimpleAdmin.Api/Program.cs` - Added SetEndSessionEndpointUris + EnableEndSessionEndpointPassthrough
- `SimpleAdmin.Api/Workers/OpenIddictWorker.cs` - Added Permissions.Endpoints.EndSession to SPA client
- `SimpleAdmin.Api/Controllers/AuthorizationController.cs` - Added Logout() action at ~/connect/logout

**SPA (created):**
- `simple-admin-spa/src/auth/oidcManager.ts` - UserManager singleton with PKCE config targeting localhost:5000
- `simple-admin-spa/src/stores/authStore.ts` - Pinia store with initialize/login/logout/setUser
- `simple-admin-spa/src/router/index.ts` - Routes + beforeEach auth guard (public meta pattern)
- `simple-admin-spa/src/views/CallbackView.vue` - OIDC callback handler, redirects to /users or /oidc-error
- `simple-admin-spa/src/views/OidcErrorView.vue` - Error page with reason display and Try again button
- `simple-admin-spa/src/views/UsersView.vue` - Placeholder protected page
- `simple-admin-spa/src/components/AppShell.vue` - PrimeVue Toolbar with SimpleAdmin title and Logout button
- `simple-admin-spa/src/App.vue` - Conditional AppShell wrapper based on auth state
- `simple-admin-spa/src/main.ts` - Pinia -> Router -> PrimeVue registration order

## Decisions Made

- **OpenIddict v7 API names:** `SetEndSessionEndpointUris` and `EnableEndSessionEndpointPassthrough` — the `Logout` variants do not exist in v7. Permission constant is `Endpoints.EndSession` (value: `"ept:end_session"`).
- **useAuthStore import location:** Imported at module top level in router/index.ts (which is valid since it is only *called* inside beforeEach, not at import time). This avoids the need for dynamic imports.
- **Pinia registration order:** `app.use(pinia)` before `app.use(router)` is critical for guard access to stores.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Corrected OpenIddict v7 API names for end-session endpoint**
- **Found during:** Task 1 (Add OpenIddict logout endpoint to backend)
- **Issue:** Plan specified `SetLogoutEndpointUris`, `EnableLogoutEndpointPassthrough`, and `Permissions.Endpoints.Logout` — none of these exist in OpenIddict v7.3.0. The build failed with 3 CS1061/CS0117 errors.
- **Fix:** Used correct v7 names: `SetEndSessionEndpointUris`, `EnableEndSessionEndpointPassthrough`, `Permissions.Endpoints.EndSession`
- **Files modified:** SimpleAdmin.Api/Program.cs, SimpleAdmin.Api/Workers/OpenIddictWorker.cs
- **Verification:** dotnet build passes with 0 errors after fix
- **Committed in:** 53a10bf (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 - bug in plan's API names for OpenIddict v7)
**Impact on plan:** Essential correction — plan documented the wrong constant names for OpenIddict v7. All three API names discovered from the package's XML documentation and confirmed by reflection on the abstractions DLL. No scope creep.

## Issues Encountered

- OpenIddict v7 renamed the "Logout" endpoint terminology to "EndSession" throughout its API. The research document noted this as an open question; the build errors confirmed `EndSession` is the correct name across all three locations.

## User Setup Required

None - no external service configuration required. The SPA runs locally on localhost:5173 against the .NET backend on localhost:5000.

## Next Phase Readiness

- Backend logout endpoint configured and backend builds cleanly
- Vue 3 SPA builds with `npm run build` — 0 errors, 0 warnings
- All OIDC infrastructure in place: oidcManager, authStore, router guard, callback handling, app shell with logout
- Plan 03-02 can proceed: `@hey-api/openapi-ts` client generation and typed API client setup

---
*Phase: 03-vue-spa-oidc*
*Completed: 2026-03-11*
