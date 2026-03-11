# Phase 1: Backend Foundation - Context

**Gathered:** 2026-03-11
**Status:** Ready for planning

<domain>
## Phase Boundary

A running .NET 10 host that issues authorization codes and access tokens via Authorization Code + PKCE, protecting its own API endpoints with OpenIddict token validation. Includes EF Core in-memory setup with Identity + OpenIddict tables, OpenIddict server configuration, a Razor login page, and a protected test endpoint.

</domain>

<decisions>
## Implementation Decisions

### Login page appearance
- Minimal centered form — clean white card centered on the page, no framework CSS dependency (no Bootstrap, no PrimeVue CSS)
- "SimpleAdmin" title as a heading above the form, no logo or tagline
- Email + Password fields only — no "remember me" checkbox, no "forgot password" link
- Single red/orange error banner above the form for invalid credentials ("Invalid email or password")
- Submit button labeled "Sign In"

### Identity configuration
- Use `AddIdentity<ApplicationUser, IdentityRole>` (not `AddDefaultIdentity`) to avoid default UI conflicts with OpenIddict
- Pass shared `InMemoryDatabaseRoot` singleton to all `UseInMemoryDatabase()` calls to avoid data isolation across DbContext instances

### Auth scheme
- Use `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme` as default auth scheme, not JwtBearer

### Claude's Discretion
- Project structure and solution layout
- OpenIddict client seed configuration (redirect URIs, scopes, token lifetimes)
- Which protected test endpoint to scaffold for Bearer token validation
- Razor page layout/CSS implementation details beyond the decisions above

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- None — greenfield project, no existing code

### Established Patterns
- None — patterns will be established by this phase

### Integration Points
- Phase 2 (REST API Contract) will add UsersController on top of this foundation
- Phase 3 (Vue SPA + OIDC) will authenticate against this OpenIddict server using oidc-client-ts

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 01-backend-foundation*
*Context gathered: 2026-03-11*
