# Phase 3: Vue SPA + OIDC - Research

**Researched:** 2026-03-11
**Domain:** Vue 3 SPA, oidc-client-ts PKCE, @hey-api/openapi-ts client generation, PrimeVue 4
**Confidence:** HIGH (core stack verified via official docs and npm registry)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Token expiry: 401 from API triggers immediate redirect to login (no toast, no silent refresh)
- Clear tokens and redirect — no intermediate screen
- API client interceptor handles 401 only (not 403) — POC has no role-based restrictions
- OIDC callback errors: show a simple error page with the reason and a "Try again" button that triggers a new login attempt
- Logout button placement: top-right of the app shell (PrimeVue Toolbar or Menubar)
- Display: plain "Logout" button only — no user email or info displayed
- No confirmation dialog — click logout triggers immediately
- Server-side logout via oidc-client-ts signoutRedirect() through the OpenIddict end-session endpoint
- Post-logout redirect to login page (backend PostLogoutRedirectUri is localhost:5173/)

### Claude's Discretion
- Post-login landing route (expected: /users based on success criteria)
- App shell layout structure (topbar vs sidebar — topbar implied by logout placement)
- PrimeVue theme selection and configuration
- Error page component design for callback failures
- Pinia store structure and naming
- Vue Router route naming and path conventions

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| AUTH-02 | User can log out and is redirected to the login page with tokens cleared | oidc-client-ts signoutRedirect(), backend logout endpoint, Pinia store clearUser, Vue Router guard |
</phase_requirements>

---

## Summary

Phase 3 builds a greenfield Vue 3 SPA using Vite with Authorization Code + PKCE authentication against the existing .NET OpenIddict backend. The standard stack is oidc-client-ts 3.4.1 for OIDC protocol handling, Pinia for auth state, Vue Router 4 for navigation and auth guards, PrimeVue 4 for UI components, and @hey-api/openapi-ts 0.93.x for generating a typed API client from the backend's OpenAPI spec.

**Critical finding:** The current backend `AuthorizationController.cs` handles `/connect/authorize` and `/connect/token` but has NO logout endpoint. Implementing `signoutRedirect()` in the SPA requires adding a backend logout endpoint (`/connect/logout`) with `SetLogoutEndpointUris`, `EnableLogoutEndpointPassthrough()`, and the permission `OpenIddictConstants.Permissions.Endpoints.EndSession` seeded in `OpenIddictWorker`. This is a backend task within Phase 3.

**Primary recommendation:** Scaffold with `npm create vue@latest` (TypeScript + Vue Router + Pinia options enabled), then install oidc-client-ts + PrimeVue 4 + @hey-api/openapi-ts. Add the backend logout endpoint as the first task in Plan 03-01 (it unblocks testing logout in 03-02).

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| vite | 6.x (via `npm create vue`) | Build tool, dev server | Official Vue 3 recommendation; HMR, TypeScript, proxy built-in |
| vue | 3.5.x | Framework | Locked by project |
| vue-router | 4.x | SPA routing + auth guards | Official Vue 3 router |
| pinia | 2.x | Auth state store | Official Vue 3 state management |
| oidc-client-ts | 3.4.1 | OIDC Authorization Code + PKCE | Locked in project decisions; maintained TypeScript-native library |
| primevue | 4.x | UI component library | Locked by project (USER-01-04 use PrimeVue DataTable) |
| @primeuix/themes | 1.x | PrimeVue 4 theme preset (Aura/Lara) | Required for PrimeVue 4 styled mode |
| typescript | 5.x | Language | Locked by project scaffold |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| @hey-api/openapi-ts | 0.93.x (pin with -E) | Generate typed API client from OpenAPI spec | Code generation at build time |
| @hey-api/client-fetch | latest compatible | Runtime Fetch-based HTTP client for generated code | Peer of openapi-ts when using Fetch client |
| primeicons | 7.x | Icon set for PrimeVue | Needed by Button, Toolbar etc. |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| oidc-client-ts | vue3-oidc | vue3-oidc wraps oidc-client-ts; adds abstraction but less control over callback routing |
| @hey-api/client-fetch | axios client | Fetch client is zero-dependency; axios adds weight. Fetch sufficient for POC |
| PrimeVue Aura theme | Lara theme | Both work; Aura is primary default in PrimeVue 4 docs; pick either |

**Installation:**
```bash
# Scaffold (interactive — select TypeScript, Vue Router, Pinia)
npm create vue@latest simple-admin-spa

cd simple-admin-spa

# OIDC + UI
npm install oidc-client-ts primevue @primeuix/themes primeicons

# API client generator (pinned exact version, dev-only)
npm install @hey-api/openapi-ts -D -E
npm install @hey-api/client-fetch
```

---

## Architecture Patterns

### Recommended Project Structure
```
simple-admin-spa/
├── src/
│   ├── auth/
│   │   └── oidcManager.ts    # UserManager singleton + UserManagerSettings
│   ├── stores/
│   │   └── authStore.ts      # Pinia store wrapping oidc-client-ts User
│   ├── router/
│   │   └── index.ts          # Vue Router + beforeEach auth guard
│   ├── views/
│   │   ├── CallbackView.vue  # /callback — runs signinRedirectCallback
│   │   ├── OidcErrorView.vue # OIDC callback error page
│   │   └── UsersView.vue     # /users — protected landing page
│   ├── components/
│   │   └── AppShell.vue      # PrimeVue Toolbar with Logout button
│   ├── client/               # Generated by @hey-api/openapi-ts (do not edit)
│   ├── App.vue
│   └── main.ts
├── openapi-ts.config.ts      # hey-api generation config
├── vite.config.ts
└── package.json
```

### Pattern 1: UserManager Singleton
**What:** Single `UserManager` instance created at module init, imported wherever needed
**When to use:** Always — oidc-client-ts manages state stores internally; multiple instances cause state conflicts
**Example:**
```typescript
// src/auth/oidcManager.ts
// Source: https://authts.github.io/oidc-client-ts/interfaces/UserManagerSettings.html
import { UserManager, WebStorageStateStore } from 'oidc-client-ts'

export const userManager = new UserManager({
  authority: 'http://localhost:5000',        // .NET host base URL
  client_id: 'simple-admin-spa',            // matches OpenIddictWorker seed
  redirect_uri: 'http://localhost:5173/callback',
  post_logout_redirect_uri: 'http://localhost:5173/',
  response_type: 'code',
  scope: 'openid email profile api',
  // sessionStorage is the default userStore — explicit for clarity
  userStore: new WebStorageStateStore({ store: window.sessionStorage }),
  // POC: no silent renew (token expiry triggers login redirect instead)
  automaticSilentRenew: false,
})
```

### Pattern 2: Pinia authStore
**What:** Thin Pinia store that hydrates from `userManager.getUser()` and exposes login/logout/isAuthenticated
**When to use:** Always — components should not import `userManager` directly; store is the single source of truth for auth state
**Example:**
```typescript
// src/stores/authStore.ts
// Source: https://pinia.vuejs.org/introduction.html
import { defineStore } from 'pinia'
import { userManager } from '@/auth/oidcManager'
import type { User } from 'oidc-client-ts'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as User | null,
  }),
  getters: {
    isAuthenticated: (state) => !!state.user && !state.user.expired,
    accessToken: (state) => state.user?.access_token ?? null,
  },
  actions: {
    async initialize() {
      this.user = await userManager.getUser()
    },
    async login() {
      await userManager.signinRedirect()
    },
    async logout() {
      await userManager.signoutRedirect()
    },
    setUser(user: User | null) {
      this.user = user
    },
  },
})
```

### Pattern 3: Vue Router Auth Guard
**What:** Global `beforeEach` guard that checks authStore; redirects unauthenticated users to login via `userManager.signinRedirect()`
**When to use:** All routes except `/callback` and `/oidc-error` are protected
**Example:**
```typescript
// src/router/index.ts
// Source: https://router.vuejs.org/guide/advanced/navigation-guards.html
import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/callback', component: () => import('@/views/CallbackView.vue'), meta: { public: true } },
    { path: '/oidc-error', component: () => import('@/views/OidcErrorView.vue'), meta: { public: true } },
    { path: '/users', component: () => import('@/views/UsersView.vue') },
    { path: '/', redirect: '/users' },
  ],
})

router.beforeEach(async (to) => {
  if (to.meta.public) return true
  const auth = useAuthStore()
  // Call initialize() to hydrate from sessionStorage on first load
  if (!auth.user) await auth.initialize()
  if (!auth.isAuthenticated) {
    await auth.login()   // triggers signinRedirect — never returns
    return false
  }
  return true
})

export default router
```

**CRITICAL:** Call `useAuthStore()` inside the guard function body (not at module top level). Pinia is not available before `app.use(pinia)` completes; calling stores at module top level causes "Cannot access store before initialization" error.

### Pattern 4: OIDC Callback Route
**What:** `/callback` route that calls `signinRedirectCallback()` and redirects to `/users`
**When to use:** Always — must be mounted as its own route to avoid auth guard interference
**Example:**
```typescript
// src/views/CallbackView.vue
// Source: https://authts.github.io/oidc-client-ts/classes/UserManager.html
<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { userManager } from '@/auth/oidcManager'
import { useAuthStore } from '@/stores/authStore'

const router = useRouter()
const auth = useAuthStore()

onMounted(async () => {
  try {
    const user = await userManager.signinRedirectCallback()
    auth.setUser(user)
    await router.replace('/users')
  } catch (err) {
    const reason = err instanceof Error ? err.message : String(err)
    await router.replace({ path: '/oidc-error', query: { reason } })
  }
})
</script>

<template><div>Completing login...</div></template>
```

### Pattern 5: hey-api Fetch Client with Bearer Interceptor
**What:** Configure generated `client` with `baseUrl` and a request interceptor that injects the Bearer token; add a response interceptor for 401 handling
**When to use:** Set up once in `main.ts` after authStore is created
**Example:**
```typescript
// main.ts (setup section)
// Source: https://heyapi.dev/openapi-ts/clients/fetch
import { client } from '@/client/client.gen'
import { useAuthStore } from '@/stores/authStore'

client.setConfig({ baseUrl: 'http://localhost:5000' })

client.interceptors.request.use((request) => {
  const auth = useAuthStore()
  if (auth.accessToken) {
    request.headers.set('Authorization', `Bearer ${auth.accessToken}`)
  }
  return request
})

client.interceptors.response.use(async (response) => {
  if (response.status === 401) {
    const auth = useAuthStore()
    auth.setUser(null)
    await auth.login()  // triggers signinRedirect
  }
  return response
})
```

### Pattern 6: hey-api Code Generation Config
**What:** `openapi-ts.config.ts` that points at the live backend OpenAPI spec and outputs to `src/client/`
**Example:**
```typescript
// openapi-ts.config.ts
// Source: https://heyapi.dev/openapi-ts/configuration
import { defineConfig } from '@hey-api/openapi-ts'

export default defineConfig({
  input: 'http://localhost:5000/openapi/v1.json',
  output: {
    path: 'src/client',
    format: 'prettier',
  },
  client: '@hey-api/client-fetch',
})
```

Add to `package.json` scripts:
```json
"openapi-ts": "openapi-ts"
```

Run: `npm run openapi-ts` (backend must be running).

### Pattern 7: PrimeVue 4 Setup (main.ts)
**What:** Register PrimeVue plugin with Aura preset
**Example:**
```typescript
// main.ts
// Source: https://primevue.org/vite
import { createApp } from 'vue'
import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura'
import 'primeicons/primeicons.css'
import App from './App.vue'
import router from './router'
import { createPinia } from 'pinia'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)
app.use(PrimeVue, { theme: { preset: Aura } })

app.mount('#app')
```

**Note:** `app.use(pinia)` must come BEFORE `app.use(router)` so Pinia stores are available in the router's `beforeEach` guard.

### Anti-Patterns to Avoid
- **Calling `useAuthStore()` at module top level in router/index.ts:** Pinia is not installed yet. Always call inside `beforeEach` callback.
- **Multiple UserManager instances:** Creates separate session stores; OIDC state is lost between instances. Export a singleton.
- **Accessing `userManager` directly from Vue components:** Go through authStore for consistent reactive state.
- **Not marking `/callback` as public in router meta:** Guard will loop-redirect before `signinRedirectCallback()` can complete.
- **Not setting `automaticSilentRenew: false`:** Default is `true`; POC has no silent renew iframe set up and it will throw errors on token expiry.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| PKCE code generation + exchange | Custom crypto + token fetch | oidc-client-ts `signinRedirect` | Code verifier entropy, state parameter CSRF, token validation — all complex |
| Typed API client | Manual `fetch` wrappers | @hey-api/openapi-ts generated client | Schema drift, missing error types, no type safety on response bodies |
| Session storage user hydration | Custom localStorage/sessionStorage parsing | oidc-client-ts `userStore` + `getUser()` | Token expiry math, serialization format, cross-tab consistency |
| Discovery document fetch | Hardcoding OIDC endpoints | oidc-client-ts `authority` auto-discovery via `.well-known/openid-configuration` | Endpoint URLs may change; library fetches and caches discovery doc automatically |

**Key insight:** oidc-client-ts handles the entire browser-side OIDC protocol (code challenge generation, state nonce, discovery, token validation). Any custom re-implementation misses edge cases that cause security vulnerabilities or token rejection.

---

## Critical Backend Gap: Logout Endpoint

The current backend does NOT expose a logout/end-session endpoint. `oidc-client-ts` `signoutRedirect()` calls the `end_session_endpoint` from the discovery document. Without it, logout silently fails.

### Required Backend Changes (part of Plan 03-01)

**1. Program.cs — server options:**
```csharp
options.SetAuthorizationEndpointUris("connect/authorize")
       .SetTokenEndpointUris("connect/token")
       .SetLogoutEndpointUris("connect/logout");   // ADD THIS

options.UseAspNetCore()
       .EnableAuthorizationEndpointPassthrough()
       .EnableTokenEndpointPassthrough()
       .EnableLogoutEndpointPassthrough();          // ADD THIS
```

**2. OpenIddictWorker.cs — SPA client permissions:**
```csharp
// ADD to the Permissions set:
OpenIddictConstants.Permissions.Endpoints.Logout,
```

**3. AuthorizationController.cs — logout action:**
```csharp
[HttpGet("~/connect/logout")]
[HttpPost("~/connect/logout")]
public async Task<IActionResult> Logout()
{
    // Sign out Identity cookie session
    await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

    // Signal OpenIddict to complete end-session and redirect to post_logout_redirect_uri
    return SignOut(
        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        properties: new AuthenticationProperties { RedirectUri = "/" }
    );
}
```

**Source confidence:** MEDIUM — pattern assembled from official OpenIddict migration docs and community samples. The `IdentityConstants.ApplicationScheme` SignOut + `OpenIddictServerAspNetCoreDefaults.AuthenticationScheme` SignOut combination follows the same dual-scheme pattern used by the existing `Authorize` action.

---

## Common Pitfalls

### Pitfall 1: Pinia Store Accessed Before Installation
**What goes wrong:** `useAuthStore()` called at router module top level throws "getActivePinia() was called with no active Pinia"
**Why it happens:** Router module is evaluated before `app.use(pinia)` runs
**How to avoid:** Always call `useAuthStore()` inside `beforeEach(async (to) => { ... })` function body
**Warning signs:** Error on first navigation, works on subsequent navigations (state already initialized)

### Pitfall 2: OIDC Callback Route Intercepted by Auth Guard
**What goes wrong:** Navigating to `/callback` triggers auth guard, which calls `signinRedirect()` again, creating an infinite loop
**Why it happens:** Guard sees user as unauthenticated (callback not yet processed) and redirects before `signinRedirectCallback()` can run
**How to avoid:** Mark `/callback` and `/oidc-error` with `meta: { public: true }` and short-circuit guard for those routes
**Warning signs:** Browser address bar flickers between `/callback?code=...` and the login redirect URL

### Pitfall 3: OpenIddict End-Session Endpoint Not Configured
**What goes wrong:** `signoutRedirect()` from oidc-client-ts either fails silently or throws "No end_session_endpoint in discovery document"
**Why it happens:** The current backend has no logout endpoint registered; OpenIddict will not advertise it in `/.well-known/openid-configuration`
**How to avoid:** Add `SetLogoutEndpointUris` + `EnableLogoutEndpointPassthrough` + logout controller action BEFORE testing logout
**Warning signs:** Discovery document at `http://localhost:5000/.well-known/openid-configuration` missing `end_session_endpoint` key

### Pitfall 4: Access Token Not Reaching API (CORS)
**What goes wrong:** API calls fail with CORS error; Authorization header is stripped
**Why it happens:** CORS policy is already configured on the backend for `http://localhost:5173` with `AllowAnyHeader()`; confirmed in Program.cs — this pitfall is pre-mitigated
**How to avoid:** Verify `UseCors()` call order in Program.cs is BEFORE `UseAuthentication()` — already correct in existing code
**Warning signs:** Browser console shows CORS preflight failure

### Pitfall 5: SessionStorage Not Persisting Across Tabs
**What goes wrong:** Opening SPA in a second tab requires re-login
**Why it happens:** `sessionStorage` is tab-scoped by browser spec
**How to avoid:** This is accepted behavior for a POC — CONTEXT.md decision locks sessionStorage. Document it; don't fix it
**Warning signs:** User complains about logging in again in new tab (this is expected behavior)

### Pitfall 6: @hey-api/openapi-ts Breaking Changes Without Pinning
**What goes wrong:** `npm update` pulls a new minor/patch that changes generated output format
**Why it happens:** hey-api explicitly says "initial development" semver — breaking changes in any release
**How to avoid:** Install with `-E` (exact) flag as noted in project decisions: `npm install @hey-api/openapi-ts -D -E`
**Warning signs:** Generated `src/client/` files change shape after npm install

### Pitfall 7: Backend Must Be Running for Code Generation
**What goes wrong:** `npm run openapi-ts` fails with connection refused
**Why it happens:** Config uses `http://localhost:5000/openapi/v1.json` as input; backend must serve it
**How to avoid:** Start the .NET backend first, then run code generation. Alternative: copy the JSON locally
**Warning signs:** `ECONNREFUSED` in openapi-ts output

### Pitfall 8: PrimeVue 4 Requires @primeuix/themes (Not Old primevue/themes)
**What goes wrong:** Importing `from 'primevue/themes/aura'` causes module not found error
**Why it happens:** PrimeVue 4 moved theme presets to the separate `@primeuix/themes` package
**How to avoid:** `npm install @primeuix/themes` and import `from '@primeuix/themes/aura'`
**Warning signs:** Build error on PrimeVue configuration in main.ts

---

## Code Examples

### Full UserManagerSettings for This Project
```typescript
// Source: https://authts.github.io/oidc-client-ts/interfaces/UserManagerSettings.html
import { UserManager, WebStorageStateStore } from 'oidc-client-ts'

export const userManager = new UserManager({
  authority: 'http://localhost:5000',
  client_id: 'simple-admin-spa',
  redirect_uri: 'http://localhost:5173/callback',
  post_logout_redirect_uri: 'http://localhost:5173/',
  response_type: 'code',
  scope: 'openid email profile api',
  userStore: new WebStorageStateStore({ store: window.sessionStorage }),
  automaticSilentRenew: false,
  loadUserInfo: false,
})
```

### hey-api Bearer Token + 401 Redirect
```typescript
// Source: https://heyapi.dev/openapi-ts/clients/fetch
client.interceptors.request.use((request) => {
  const token = useAuthStore().accessToken
  if (token) request.headers.set('Authorization', `Bearer ${token}`)
  return request
})

client.interceptors.response.use(async (response) => {
  if (response.status === 401) {
    useAuthStore().setUser(null)
    await userManager.signinRedirect()
  }
  return response
})
```

### OpenIddict Logout Endpoint (Backend)
```csharp
// Source: assembled from OpenIddict docs + community patterns
// https://documentation.openiddict.com/integrations/aspnet-core
[HttpGet("~/connect/logout")]
[HttpPost("~/connect/logout")]
public async Task<IActionResult> Logout()
{
    await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    return SignOut(
        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
        properties: new AuthenticationProperties { RedirectUri = "/" }
    );
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| oidc-client-js (deprecated) | oidc-client-ts | 2022 | TypeScript-native; oidc-client-js unmaintained |
| PrimeVue 3 themes via CSS link | PrimeVue 4 + @primeuix/themes preset | PrimeVue 4.0 (2024) | Design token system; no CSS import needed |
| Vuex | Pinia | Vue 3 era (2021+) | Pinia is official; Vuex in maintenance mode |
| vue-router implicit meta | Explicit `meta: { public: true }` pattern | Ongoing | Opt-in public routes are clearer than opt-out |

**Deprecated/outdated:**
- `oidc-client-js`: Unmaintained. `oidc-client-ts` is the maintained fork.
- `vuex-oidc`: Built on Vuex, not Pinia. Do not use for Vue 3 projects.
- PrimeVue 3 `primevue/themes/saga` etc: Old CSS theme system replaced by @primeuix/themes design tokens.

---

## Open Questions

1. **OpenIddict `end_session_endpoint` in discovery document**
   - What we know: Discovery doc at `/.well-known/openid-configuration` is auto-generated by OpenIddict; it lists `end_session_endpoint` only when `SetLogoutEndpointUris` is configured
   - What's unclear: Whether the existing backend will expose `end_session_endpoint` in the discovery doc before the logout endpoint is added — it will NOT, confirmed by search
   - Recommendation: Plan 03-01 Task 1 must add backend logout endpoint; verify by fetching discovery doc before proceeding to SPA logout implementation

2. **Vite dev server proxy for API calls**
   - What we know: CORS is configured on the backend for `http://localhost:5173`; direct calls to `http://localhost:5000` will work
   - What's unclear: Whether team prefers a Vite proxy (`/api` → `localhost:5000/api`) to avoid exposing backend origin in SPA config
   - Recommendation: Use direct URL for POC simplicity; no proxy needed since CORS headers are already set

3. **OpenIddict `Permissions.Endpoints.Logout` vs `Permissions.Endpoints.EndSession`**
   - What we know: OpenIddict renamed "Logout" to "End-session" in v6.0; both constant names may exist in v7.3.0
   - What's unclear: Which constant name is correct in v7.3.0 — `Permissions.Endpoints.Logout` or `Permissions.Endpoints.EndSession`
   - Recommendation: Check IntelliSense/source during implementation; the STATE.md decision log pattern is to document the correct constant after hitting the issue

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright (e2e, to be added) |
| Config file | `playwright.config.ts` — Wave 0 gap |
| Quick run command | `npx playwright test --project=chromium login.spec.ts` |
| Full suite command | `npx playwright test` |

**Note:** The existing test suite is in `SimpleAdmin.Tests` (.NET xUnit). Vue SPA tests will use Playwright for browser-level smoke tests of the full auth flow (login redirect → callback → protected route → logout). Vitest is an option for unit testing Pinia stores (not required for Phase 3 scope — auth smoke test covers AUTH-02 end-to-end).

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| AUTH-02 | Clicking logout clears tokens and redirects to login page | e2e smoke | `npx playwright test --project=chromium -g "logout"` | Wave 0 gap |
| AUTH-02 | Accessing a protected SPA route after logout triggers login redirect | e2e smoke | `npx playwright test --project=chromium -g "protected route after logout"` | Wave 0 gap |
| AUTH-02 (success criterion 1) | SPA with no session redirects to Razor login page | e2e smoke | `npx playwright test --project=chromium -g "unauthenticated redirect"` | Wave 0 gap |
| AUTH-02 (success criterion 2) | Valid credentials redirect back to SPA /users | e2e smoke | `npx playwright test --project=chromium -g "login flow"` | Wave 0 gap |
| AUTH-02 (success criterion 3) | Authenticated session persists across page refresh | e2e smoke | `npx playwright test --project=chromium -g "session persistence"` | Wave 0 gap |
| AUTH-02 (success criterion 5) | GET /api/users includes Authorization: Bearer header | e2e smoke | `npx playwright test --project=chromium -g "api bearer"` | Wave 0 gap |

### Sampling Rate
- **Per task commit:** Manual browser verification (SPA not yet unit-testable at task granularity)
- **Per wave merge:** `npx playwright test --project=chromium`
- **Phase gate:** Full Playwright smoke suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `simple-admin-spa/playwright.config.ts` — Playwright config, baseURL `http://localhost:5173`
- [ ] `simple-admin-spa/tests/auth.spec.ts` — covers all AUTH-02 success criteria
- [ ] Framework install: `npm install @playwright/test -D && npx playwright install chromium` in SPA project

---

## Sources

### Primary (HIGH confidence)
- `https://authts.github.io/oidc-client-ts/interfaces/UserManagerSettings.html` — all UserManagerSettings properties and defaults
- `https://authts.github.io/oidc-client-ts/classes/UserManager.html` — UserManager class API
- `https://heyapi.dev/openapi-ts/clients/fetch` — Fetch client setup, interceptors, setConfig
- `https://heyapi.dev/openapi-ts/get-started` — installation, config file, npx usage
- `https://pinia.vuejs.org/introduction.html` — Pinia defineStore, outside component usage
- `https://router.vuejs.org/guide/advanced/navigation-guards.html` — beforeEach guard
- npm registry — oidc-client-ts@3.4.1, @hey-api/openapi-ts@0.93.x confirmed latest

### Secondary (MEDIUM confidence)
- `https://documentation.openiddict.com/` + community samples — logout endpoint configuration pattern (`SetLogoutEndpointUris`, `EnableLogoutEndpointPassthrough`)
- `https://primevue.org/vite` + npm package page — PrimeVue 4 Vite setup, @primeuix/themes Aura preset
- OpenIddict GitHub issues #823, #312 — SignOut dual-scheme pattern for logout

### Tertiary (LOW confidence)
- `https://andreyka26.com/openid-connect-authorization-code-using-openiddict-and-dot-net` — logout Program.cs configuration (unverified against v7.3.0 specifically; shape matches known API)

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all versions verified via npm registry and official docs
- Architecture patterns: HIGH — patterns from official oidc-client-ts docs and Vue/Pinia official docs
- Backend logout endpoint: MEDIUM — pattern from OpenIddict docs but exact constant name for v7.3.0 needs IntelliSense verification
- Pitfalls: HIGH — Pinia-in-router and OIDC callback route pitfalls are well-documented community issues

**Research date:** 2026-03-11
**Valid until:** 2026-04-10 (stable libraries; oidc-client-ts and PrimeVue 4 are not fast-moving right now)
