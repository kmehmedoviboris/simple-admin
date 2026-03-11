# Roadmap: SimpleAdmin

## Overview

Four sequentially dependent phases deliver a working end-to-end admin panel. The backend authorization server must be stable before the REST API can be verified, the OpenAPI contract must exist before the Vue client can be generated, and the OIDC round-trip must be proven in the SPA before CRUD views are layered on top. Each phase produces a narrowly verifiable outcome that unblocks the next.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Backend Foundation** - OpenIddict authorization server, Identity + EF Core wiring, Razor login page, and protected API scaffolding
- [ ] **Phase 2: REST API Contract** - UsersController with CRUD endpoints, OpenAPI spec generation, and seeded test data
- [ ] **Phase 3: Vue SPA + OIDC** - Vue 3 project scaffold, oidc-client-ts PKCE integration, auth store, router guard, and verified login/logout
- [ ] **Phase 4: User CRUD Views** - DataTable list view, create/edit forms, delete confirmation, inline validation, loading states, and error toast feedback

## Phase Details

### Phase 1: Backend Foundation
**Goal**: A running .NET 10 host that issues authorization codes and access tokens via Authorization Code + PKCE, protecting its own API endpoints with OpenIddict token validation
**Depends on**: Nothing (first phase)
**Requirements**: AUTH-01, AUTH-03
**Success Criteria** (what must be TRUE):
  1. Navigating to the authorize endpoint in a browser redirects to the Razor login page; submitting valid credentials redirects back with an authorization code
  2. Exchanging the authorization code for a token at `/connect/token` returns a valid access token
  3. A GET request to a protected API endpoint with a valid Bearer token returns 200; the same request without a token returns 401
  4. A GET request to a protected API endpoint with a valid Bearer token returns 200; the same request without a token returns 401 (verifiable via curl or a REST client)
**Plans**: 4 plans

Plans:
- [ ] 01-01: EF Core in-memory DbContext with Identity + OpenIddict tables and shared InMemoryDatabaseRoot
- [ ] 01-02: OpenIddict server configuration in Program.cs (CORS, Identity, OpenIddict server + validation, OpenIddictWorker seed)
- [ ] 01-03: AuthorizationController (passthrough mode) and Razor Login Page (/Account/Login)
- [ ] 01-04: End-to-end auth code flow smoke test (browser authorize → login → token exchange → protected endpoint)

### Phase 2: REST API Contract
**Goal**: A fully functional UsersController exposing CRUD endpoints, protected by OpenIddict Bearer validation, with an OpenAPI spec generated from the running server
**Depends on**: Phase 1
**Requirements**: USER-01, USER-02, USER-03, USER-04
**Success Criteria** (what must be TRUE):
  1. GET /api/users with a valid Bearer token returns a list of seeded users as JSON
  2. POST /api/users with a valid Bearer token and a valid email/password body creates a user and returns it
  3. PUT /api/users/{id} with a valid Bearer token updates email and/or password for an existing user
  4. DELETE /api/users/{id} with a valid Bearer token removes the user; subsequent GET confirms absence
  5. Navigating to `/openapi/v1.json` returns the full OpenAPI spec document for the running API
**Plans**: 4 plans

Plans:
- [ ] 02-01: ApplicationUser DTOs (UserListDto, CreateUserDto, UpdateUserDto), UsersController with GET/POST/PUT/DELETE, UserManager integration
- [ ] 02-02: Startup user seed (admin + test users), OpenAPI document configuration (Scalar dev UI), end-to-end REST smoke test

### Phase 3: Vue SPA + OIDC
**Goal**: A Vite-built Vue 3 SPA that completes Authorization Code + PKCE login against the .NET host, stores the access token, and exposes a typed API client ready for CRUD views
**Depends on**: Phase 2
**Requirements**: AUTH-02
**Success Criteria** (what must be TRUE):
  1. Opening the SPA in a browser with no session redirects automatically to the Razor login page
  2. Submitting valid credentials on the login page redirects back to the SPA at `/users` with the user authenticated
  3. The authenticated session persists across a page refresh (sessionStorage store)
  4. Clicking logout clears all tokens and redirects to the login page; accessing a protected SPA route again triggers the login redirect
  5. A network request to GET /api/users from the SPA includes the Authorization: Bearer header and receives a 200 response
**Plans**: 4 plans

Plans:
- [ ] 03-01: Vite + Vue 3 + TypeScript + PrimeVue scaffold, Pinia authStore, Vue Router with auth guard, oidc-client-ts PKCE configuration
- [ ] 03-02: OIDC callback route (signinRedirectCallback → /users redirect), logout action, generated API client (hey-api/openapi-ts) with Bearer token interceptor, login/logout smoke test

### Phase 4: User CRUD Views
**Goal**: A fully working admin UI where an authenticated user can list, create, edit, and delete users — with form validation, loading indicators, and error feedback on every interaction
**Depends on**: Phase 3
**Requirements**: UX-01, UX-02, UX-03
**Success Criteria** (what must be TRUE):
  1. The user list page displays all users in a PrimeVue DataTable; a loading spinner is visible while the API request is in flight
  2. Submitting the create form with an invalid email or empty required field shows an inline validation error without calling the API
  3. Submitting a valid create or edit form shows a loading state on the submit button and navigates away on success
  4. Clicking delete on a user row shows a PrimeVue confirmation dialog; confirming removes the user from the list
  5. When the API returns a 4xx or 5xx response, a PrimeVue toast notification with the error message appears
**Plans**: 4 plans

Plans:
- [ ] 04-01: UserListView.vue with PrimeVue DataTable, loading state, and delete confirmation (ConfirmDialog)
- [ ] 04-02: UserForm.vue shared component with inline validation, UserCreateView.vue and UserEditView.vue wired to the API client, toast error feedback (Toast)

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Backend Foundation | 2/4 | In Progress|  |
| 2. REST API Contract | 0/2 | Not started | - |
| 3. Vue SPA + OIDC | 0/2 | Not started | - |
| 4. User CRUD Views | 0/2 | Not started | - |
