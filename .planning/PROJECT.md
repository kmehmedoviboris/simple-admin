# SimpleAdmin

## What This Is

A proof-of-concept web application for basic user management (CRUD). It uses a .NET 10 Web API backend with OpenIddict issuing its own tokens via Authorization Code flow, Microsoft Identity for user storage, and a Vue 3 SPA (Composition API) with PrimeVue as the admin UI. The Vue app consumes an OpenAPI-generated client. Entity Framework uses an in-memory database.

## Core Value

A working end-to-end auth code flow login that lands in a Vue 3 SPA where you can list, create, edit, and delete users.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Authorization Code flow login via OpenIddict (self-issued tokens)
- [ ] Single Razor page for the login/consent UI
- [ ] User CRUD (list, create, edit, delete) with email + password fields
- [ ] Vue 3 Composition API SPA with PrimeVue components
- [ ] OpenAPI spec on the .NET API; Vue client auto-generated from it
- [ ] Microsoft Identity for user/role storage
- [ ] Entity Framework with in-memory DbContext
- [ ] Basic layout: login → user list → create/edit/delete

### Out of Scope

- Role management UI — not needed for POC
- User profile/dashboard pages — just user CRUD
- Persistent database — in-memory only for POC
- Email verification / password reset — POC scope
- Mobile responsiveness — desktop-only POC
- Deployment / CI/CD — local dev only

## Context

- This is a proof of concept, not production code
- .NET 10 (latest preview) with OpenIddict 6.x
- Vue 3 with Vite, Composition API, PrimeVue for UI components
- OpenAPI client generation (e.g., openapi-ts or similar)
- Single solution: API project + Vue SPA project
- The API serves both the Razor login page and the REST endpoints
- The Vue SPA is a separate frontend that authenticates via the API's OpenIddict server

## Constraints

- **Stack**: .NET 10 Web API, OpenIddict, Microsoft Identity, EF Core in-memory — non-negotiable
- **Frontend**: Vue 3 Composition API + PrimeVue — non-negotiable
- **Auth flow**: Authorization Code flow with self-issued tokens — non-negotiable
- **Login UI**: Single Razor page hosted by the API — non-negotiable
- **API contract**: OpenAPI spec with generated TypeScript client — non-negotiable
- **Scope**: POC — working login + CRUD is the finish line

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| PrimeVue for UI components | User preference, rich free tier, popular with Vue 3 | — Pending |
| In-memory EF database | POC only, no persistence needed | — Pending |
| Single Razor page for login | OpenIddict auth code flow needs server-rendered login | — Pending |
| Authorization Code flow | Standard OAuth2 flow for SPAs with backend | — Pending |

---
*Last updated: 2026-03-11 after initialization*
