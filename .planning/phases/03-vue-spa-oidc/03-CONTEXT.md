# Phase 3: Vue SPA + OIDC - Context

**Gathered:** 2026-03-11
**Status:** Ready for planning

<domain>
## Phase Boundary

A Vite-built Vue 3 SPA that completes Authorization Code + PKCE login against the .NET host, stores the access token in sessionStorage, and exposes a typed API client ready for CRUD views. Includes OIDC callback handling, logout via end-session endpoint, router guard for unauthenticated routes, and an app shell with a top-bar logout button.

</domain>

<decisions>
## Implementation Decisions

### Auth failure UX
- Token expiry: 401 from API triggers immediate redirect to login (no toast, no silent refresh)
- Clear tokens and redirect — no intermediate screen
- API client interceptor handles 401 only (not 403) — POC has no role-based restrictions
- OIDC callback errors: show a simple error page with the reason and a "Try again" button that triggers a new login attempt

### Logout behavior
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

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- None — no frontend project exists yet (greenfield Vue scaffold)

### Established Patterns
- Backend OpenIddict client seed expects SPA at `localhost:5173` with callback at `/callback` and post-logout redirect to `/`
- OpenAPI spec available at `/openapi/v1.json` for client generation
- Bearer token validation via `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme`

### Integration Points
- `OpenIddictWorker.cs` seeds the SPA client with redirect URIs: `http://localhost:5173/callback` and `http://localhost:5173/`
- API endpoints at `/api/users` (GET/POST/PUT/DELETE) protected by Bearer auth
- Authorization endpoint at `/connect/authorize`, token at `/connect/token`
- OpenAPI spec at `/openapi/v1.json` for `@hey-api/openapi-ts` client generation

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 03-vue-spa-oidc*
*Context gathered: 2026-03-11*
