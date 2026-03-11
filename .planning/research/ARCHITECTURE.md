# Architecture Research

**Domain:** User Management Admin POC (.NET 10 + OpenIddict + Vue 3 SPA)
**Researched:** 2026-03-11
**Confidence:** HIGH

## Standard Architecture

### System Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│                          Browser                                      │
│                                                                       │
│  ┌─────────────────────────┐    ┌────────────────────────────────┐   │
│  │    Vue 3 SPA (Vite)     │    │  Razor Login Page              │   │
│  │                         │    │  (served by .NET API project)  │   │
│  │  - Auth Store (OIDC)    │    │  /Account/Login                │   │
│  │  - User CRUD views      │    │                                │   │
│  │  - Generated API client │    └────────────┬───────────────────┘   │
│  │  - Vue Router           │                 │ form POST              │
│  └──────────┬──────────────┘                 │                       │
│             │  fetch (Bearer token)           │                       │
└─────────────┼───────────────────────────────-┼───────────────────────┘
              │                                 │
              ▼                                 ▼
┌──────────────────────────────────────────────────────────────────────┐
│               .NET 10 Web API Project (single host)                   │
│                                                                       │
│  ┌─────────────────────────┐    ┌────────────────────────────────┐   │
│  │   REST Controllers      │    │  OpenIddict Server             │   │
│  │                         │    │                                │   │
│  │  UsersController        │    │  /connect/authorize            │   │
│  │    GET  /api/users       │    │  /connect/token                │   │
│  │    POST /api/users       │    │  /connect/logout               │   │
│  │    PUT  /api/users/{id}  │    │                                │   │
│  │    DELETE /api/users/{id}│    │  AuthorizationController       │   │
│  │                         │    │  (passthrough mode)            │   │
│  └────────────┬────────────┘    └──────────────┬─────────────────┘   │
│               │                                │                     │
│  ┌────────────▼────────────────────────────────▼─────────────────┐   │
│  │              Microsoft Identity + EF Core                      │   │
│  │                                                                │   │
│  │   UserManager<ApplicationUser>                                 │   │
│  │   SignInManager<ApplicationUser>                               │   │
│  │   ApplicationDbContext (IdentityDbContext + OpenIddict tables) │   │
│  └────────────────────────────────────────────────────────────────┘   │
│                                │                                     │
│  ┌─────────────────────────────▼──────────────────────────────────┐   │
│  │              EF Core In-Memory Database                        │   │
│  │                                                                │   │
│  │   AspNetUsers / AspNetRoles / AspNetUserRoles                  │   │
│  │   OpenIddictApplications / OpenIddictAuthorizations            │   │
│  │   OpenIddictScopes / OpenIddictTokens                          │   │
│  └────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| Vue 3 SPA | All user-facing admin UI, token management, API consumption | Vite project, Composition API, PrimeVue components, oidc-client-ts or vue-oidc-context |
| Razor Login Page | Server-rendered login form; only page the browser visits on the .NET host directly | Single Razor Page at `/Account/Login`, validates credentials via `SignInManager`, signs in with cookie |
| AuthorizationController | OpenIddict passthrough handler for `/connect/authorize` and `/connect/logout` | MVC controller; checks cookie auth, creates claims principal, calls `SignIn` with OpenIddict scheme |
| UsersController | CRUD REST endpoints for user management, protected by Bearer token | MVC controller, `[Authorize]` attribute, delegates to `UserManager` |
| OpenIddict Server | Issues authorization codes, access tokens, refresh tokens | Configured in `Program.cs`; endpoints registered at `/connect/*` |
| Microsoft Identity | User/password/role storage and validation | `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>` |
| ApplicationDbContext | Single EF Core context for both Identity tables and OpenIddict tables | Inherits `IdentityDbContext<ApplicationUser>`, configured with `options.UseOpenIddict()` |
| EF Core In-Memory DB | Ephemeral data store for POC | `UseInMemoryDatabase("SimpleAdmin")`, seeded on startup |
| Generated API Client | Type-safe HTTP client for the Vue SPA | Generated from OpenAPI spec via `@hey-api/openapi-ts` or `openapi-typescript-codegen` |

## Recommended Project Structure

```
SimpleAdmin/
├── SimpleAdmin.Api/                   # .NET 10 Web API project
│   ├── Controllers/
│   │   ├── AuthorizationController.cs # OpenIddict passthrough: /connect/authorize, /connect/logout
│   │   └── UsersController.cs         # REST CRUD: /api/users
│   ├── Data/
│   │   └── ApplicationDbContext.cs    # IdentityDbContext + OpenIddict tables
│   ├── Models/
│   │   └── ApplicationUser.cs         # Extends IdentityUser if needed
│   ├── Pages/
│   │   └── Account/
│   │       └── Login.cshtml           # Razor login page (+ Login.cshtml.cs)
│   ├── Workers/
│   │   └── OpenIddictWorker.cs        # IHostedService to seed OIDC app registration on startup
│   ├── Program.cs                     # DI setup, middleware pipeline
│   └── appsettings.json
│
└── SimpleAdmin.Spa/                   # Vue 3 + Vite project
    ├── src/
    │   ├── auth/
    │   │   ├── oidcConfig.ts          # UserManager or OidcProvider config
    │   │   └── authGuard.ts           # Vue Router navigation guard
    │   ├── api/
    │   │   ├── client.ts              # Generated client with auth interceptor attached
    │   │   └── openapi.json           # Spec fetched from /swagger/v1/swagger.json
    │   ├── stores/
    │   │   └── authStore.ts           # Pinia store: user, token, isAuthenticated
    │   ├── views/
    │   │   ├── UserListView.vue
    │   │   ├── UserCreateView.vue
    │   │   └── UserEditView.vue
    │   ├── components/
    │   │   └── UserForm.vue
    │   ├── router/
    │   │   └── index.ts               # Vue Router with auth guard on all routes
    │   ├── App.vue
    │   └── main.ts
    ├── package.json
    └── vite.config.ts
```

### Structure Rationale

- **Controllers/ vs Pages/:** Controllers handle machine-readable OAuth2 endpoints and REST API; the single Razor Page handles the human-readable login form. Keeping them separate preserves this boundary.
- **Workers/:** The OpenIddict client application registration (client_id, redirect URIs, permissions) must exist before any auth requests arrive. A hosted service seeding on startup is cleaner than seeding in `Program.cs` inline.
- **auth/ in SPA:** Isolates all OIDC ceremony (UserManager config, callback handling, guards) so views never deal with raw tokens.
- **api/client.ts (SPA):** A thin wrapper over the generated client that injects the Bearer token via request middleware. Views import from here, not directly from the generated code.

## Architectural Patterns

### Pattern 1: OpenIddict Passthrough Mode

**What:** OpenIddict validates the incoming OIDC request, then yields control to your controller action via `EnableAuthorizationEndpointPassthrough()`. Your controller decides who is logged in, what claims to include, and calls `SignIn` with the OpenIddict application scheme.

**When to use:** Always required when serving a custom login UI from the same host. OpenIddict never renders UI itself.

**Trade-offs:** Full control over claims and login UX; requires implementing the controller correctly or OIDC will return errors.

**Example:**
```csharp
// AuthorizationController.cs
[HttpGet("~/connect/authorize")]
public async Task<IActionResult> Authorize()
{
    var request = HttpContext.GetOpenIddictServerRequest()
        ?? throw new InvalidOperationException("OpenIddict request not found.");

    // If the user is not authenticated, redirect to login
    var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
    if (!result.Succeeded)
    {
        return Challenge(
            authenticationSchemes: IdentityConstants.ApplicationScheme,
            properties: new AuthenticationProperties { RedirectUri = Request.PathBase + Request.Path + QueryString.Create(Request.Query) });
    }

    var user = await _userManager.GetUserAsync(result.Principal)
        ?? throw new InvalidOperationException("User not found.");

    var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    identity.AddClaim(Claims.Subject, await _userManager.GetUserIdAsync(user));
    identity.AddClaim(Claims.Email, await _userManager.GetEmailAsync(user) ?? string.Empty);

    var principal = new ClaimsPrincipal(identity);
    principal.SetScopes(request.GetScopes());

    return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}
```

### Pattern 2: Authorization Code + PKCE Flow (Vue SPA side)

**What:** The Vue SPA uses `oidc-client-ts` (or a Vue wrapper like `vue-oidc-context`) to manage the full PKCE dance: generate code_verifier + code_challenge, redirect to `/connect/authorize`, receive the code at the callback URL, exchange it for tokens at `/connect/token`, and store the result.

**When to use:** Required for all public SPA clients. PKCE prevents code interception attacks for clients that cannot hold a secret.

**Trade-offs:** Slightly more boilerplate than implicit flow (deprecated); tokens arrive in the SPA, not a cookie — acceptable for POC scope.

**Example:**
```typescript
// auth/oidcConfig.ts
import { UserManager, WebStorageStateStore } from 'oidc-client-ts'

export const userManager = new UserManager({
  authority: 'https://localhost:5001',          // .NET API base URL
  client_id: 'simple-admin-spa',
  redirect_uri: 'http://localhost:5173/callback',
  post_logout_redirect_uri: 'http://localhost:5173',
  response_type: 'code',
  scope: 'openid profile email api',
  userStore: new WebStorageStateStore({ store: sessionStorage }),
})
```

### Pattern 3: Generated Client with Auth Middleware

**What:** The OpenAPI spec is generated from the .NET API at build time (or from a running dev server). The TypeScript client is regenerated whenever the spec changes. A thin wrapper injects the Bearer token so all API calls are automatically authenticated.

**When to use:** Always. This is the non-negotiable contract between API and SPA defined in the project constraints.

**Trade-offs:** Adds a code-gen step to the dev workflow; the generated code should not be edited manually.

**Example:**
```typescript
// api/client.ts
import { client } from './generated'  // generated by @hey-api/openapi-ts
import { userManager } from '../auth/oidcConfig'

client.interceptors.request.use(async (request) => {
  const user = await userManager.getUser()
  if (user?.access_token) {
    request.headers.set('Authorization', `Bearer ${user.access_token}`)
  }
  return request
})

export { client }
```

## Data Flow

### Auth Flow: First Login (Authorization Code + PKCE)

```
[1] User visits Vue SPA (protected route)
         |
         v
[2] authGuard detects no token → calls userManager.signinRedirect()
         |
         | Browser redirects to .NET API:
         | GET /connect/authorize?response_type=code&client_id=...&code_challenge=...
         v
[3] OpenIddict validates request → passthrough → AuthorizationController.Authorize()
         |
         | No Identity cookie found
         v
[4] AuthorizationController redirects to:
    GET /Account/Login?ReturnUrl=/connect/authorize?...
         |
         v
[5] User sees Razor Login Page, submits credentials
         |
         v
[6] Login.cshtml.cs: SignInManager.PasswordSignInAsync()
    → sets Identity application cookie
    → redirects back to /connect/authorize?...
         |
         v
[7] AuthorizationController.Authorize() runs again (cookie now present)
    → creates ClaimsIdentity with Subject, Email claims
    → sets scopes on principal
    → SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
         |
         v
[8] OpenIddict issues authorization code
    → redirects browser to:
    GET http://localhost:5173/callback?code=...&state=...
         |
         v
[9] Vue SPA /callback route: userManager.signinRedirectCallback()
    → exchanges code for tokens at POST /connect/token (includes code_verifier)
         |
         v
[10] OpenIddict token endpoint validates code + code_verifier
     → returns { access_token, id_token, refresh_token }
         |
         v
[11] oidc-client-ts stores user in sessionStorage
     authStore updated: isAuthenticated = true
     Router navigates to /users
```

### API Request Flow: Authenticated CRUD

```
[1] UserListView.vue mounts
         |
         v
[2] calls UsersService.getUsers() (generated client)
         |
         v
[3] api/client.ts interceptor: reads token from userManager.getUser()
    → sets Authorization: Bearer <access_token>
         |
         v
[4] GET /api/users → UsersController.GetUsers()
         |
         v
[5] [Authorize] attribute: OpenIddict validation stack validates Bearer token
         |
         v
[6] UserManager.GetUsersAsync() → ApplicationDbContext → In-Memory DB
         |
         v
[7] Returns List<UserDto> → JSON response → Vue component renders
```

### State Management

```
[oidc-client-ts UserManager]
        |
        | getUser() / signinRedirect() / signoutRedirect()
        v
[authStore (Pinia)]
  - user: User | null
  - isAuthenticated: boolean
  - accessToken: string | null
        |
        | read in interceptor + router guard + nav bar
        v
[Vue Components / Router Guards / API Interceptor]
```

## Suggested Build Order

The components have these dependencies that determine build order:

1. **ApplicationDbContext + Identity + OpenIddict (Data layer)**
   - Everything else depends on the DB context existing
   - Set up in-memory DB, Identity tables, OpenIddict entity sets

2. **Program.cs wiring (Composition root)**
   - Register Identity, OpenIddict server config, CORS, Swagger
   - No logic here, just registration and middleware pipeline

3. **OpenIddictWorker — seed the SPA client registration**
   - Registers client_id, redirect_uri, scopes, permissions before the host accepts requests
   - Must come before any auth requests can succeed

4. **Razor Login Page + AuthorizationController**
   - The login page needs Identity (SignInManager) wired up
   - AuthorizationController needs both Identity and OpenIddict server ready
   - These two must work together before the SPA can complete any auth flow

5. **UsersController (REST API)**
   - Depends on UserManager + OpenIddict validation (token validation)
   - Build after auth is confirmed working end-to-end

6. **OpenAPI spec generation (contract)**
   - Run once UsersController exists; generate the `openapi.json`
   - This is the handoff point between backend and frontend work

7. **Vue SPA scaffolding (Vite + PrimeVue + Router)**
   - Base project structure, layout, routing skeleton

8. **OIDC integration in SPA (oidc-client-ts)**
   - Configure UserManager, callback route, auth guard
   - Verify round-trip login works before building views

9. **Generated API client + auth interceptor**
   - Generate from spec, wrap with token injection

10. **User CRUD views (UserList, UserCreate, UserEdit)**
    - Final layer; all dependencies are in place

## Integration Points

### External Services

None — this is a self-contained POC. The .NET API is both the Authorization Server and the Resource Server.

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| Vue SPA ↔ .NET API (auth) | Browser redirects (302) + PKCE code exchange | Standard OIDC; SPA must handle /callback route |
| Vue SPA ↔ .NET API (REST) | HTTP/fetch with Bearer token header | Token injected by client.ts interceptor |
| Razor Login Page ↔ OpenIddict | Identity application cookie + ReturnUrl redirect | Login sets cookie; AuthorizationController reads it |
| AuthorizationController ↔ OpenIddict | `GetOpenIddictServerRequest()` + `SignIn()` with OpenIddict scheme | Passthrough mode contract |
| UsersController ↔ OpenIddict | OpenIddict Validation stack (auto) | `[Authorize]` triggers token validation transparently |
| ApplicationDbContext ↔ Identity + OpenIddict | Both use the same DbContext instance | Both call `options.UseOpenIddict()` and `IdentityDbContext<T>` on the same context |

## Anti-Patterns

### Anti-Pattern 1: Putting Login UI Logic in the OpenIddict Endpoint Directly

**What people do:** Try to serve the HTML login form from within the `/connect/authorize` controller action itself (inline HTML or returning a View from that action).

**Why it's wrong:** The `/connect/authorize` endpoint must redirect unauthenticated users away and accept a cookie on return. Embedding form handling in the same action conflates the OIDC protocol endpoint with the credential collection UI, making CSRF protection, ReturnUrl handling, and the "check cookie on return" logic fragile and hard to test.

**Do this instead:** Keep `/connect/authorize` (AuthorizationController) strictly as an OIDC handler. Redirect to a separate `/Account/Login` Razor Page for credential collection. The login page posts to itself, sets the Identity cookie, then redirects back to the ReturnUrl (which is the `/connect/authorize` URL). AuthorizationController then sees the cookie and proceeds.

### Anti-Pattern 2: Skipping PKCE on the SPA Client Registration

**What people do:** Register the Vue SPA client without `RequireProofKeyForCodeExchange()` because it seems simpler.

**Why it's wrong:** Without PKCE, authorization codes intercepted in transit (referrer headers, browser history, logs) can be exchanged for tokens by an attacker. For public clients (SPAs), PKCE is a required security control, not optional.

**Do this instead:** Always call `RequireProofKeyForCodeExchange()` on the server and ensure the client library (oidc-client-ts) sends code_challenge / code_verifier. This is the default behavior when using standard OIDC client libraries.

### Anti-Pattern 3: Editing the Generated API Client Manually

**What people do:** Add custom logic, fix types, or add missing endpoints directly into the generated client files.

**Why it's wrong:** Any API change will regenerate the client and overwrite manual edits. This creates merge conflicts and hidden divergence between the spec and the client code.

**Do this instead:** Keep generated files in a `/generated` subfolder committed to source but never edited. Put all customization (auth injection, error handling wrappers, typed response helpers) in a hand-written `client.ts` that imports from `/generated`.

### Anti-Pattern 4: One DbContext for Identity, Another for OpenIddict

**What people do:** Create separate DbContexts for Identity and OpenIddict to keep concerns separated.

**Why it's wrong:** EF Core in-memory provider scopes data to a single DbContext instance per named database. Splitting contexts means OpenIddict authorization/token records and Identity user records live in separate in-memory stores, breaking foreign-key relationships and making transactional consistency impossible.

**Do this instead:** Inherit from `IdentityDbContext<ApplicationUser>` and call `options.UseOpenIddict()` on the same context. Both Identity and OpenIddict share one context and one in-memory database.

## Scaling Considerations

This is a POC; scaling is explicitly out of scope. For completeness:

| Scale | Architecture Adjustments |
|-------|--------------------------|
| POC (in-memory) | Current design is appropriate. Single host, no persistence. |
| Production (persistent) | Replace in-memory DB with SQL Server/PostgreSQL. Add persistent token store. Extract CORS config. |
| Multi-tenant or high-traffic | Extract OpenIddict to dedicated Authorization Server project. Resource Server validates via introspection. Add Redis for token cache. |

## Sources

- [OpenIddict Introduction](https://documentation.openiddict.com/introduction) — Component architecture overview (HIGH confidence)
- [OpenIddict ASP.NET Core Integration](https://documentation.openiddict.com/integrations/aspnet-core) — Passthrough mode and middleware order (HIGH confidence)
- [OpenIddict Getting Started: Creating Your Own Server](https://documentation.openiddict.com/guides/getting-started/creating-your-own-server-instance) — Server setup and required components (HIGH confidence)
- [Setting up Authorization Code Flow with OpenIddict](https://dev.to/robinvanderknaap/setting-up-an-authorization-server-with-openiddict-part-iv-authorization-code-flow-3eh8) — Controller structure and data flow (MEDIUM confidence)
- [OAuth Authorization Code using OpenIddict and .NET](https://andreyka26.com/oauth-authorization-code-using-openiddict-and-dot-net) — Login Razor Page integration pattern (MEDIUM confidence)
- [OpenIddict ReactSpa WebApi PoC](https://github.com/eamsdev/OpenIddict-ReactSpa-WebApi-PoC) — SPA + Web API separation pattern (MEDIUM confidence)
- [oidc-client-ts documentation](https://authts.github.io/oidc-client-ts/) — Vue SPA PKCE client configuration (HIGH confidence)
- [Hey API openapi-ts: Middleware & Auth](https://openapi-ts.dev/openapi-fetch/middleware-auth) — Bearer token injection via request interceptor (HIGH confidence)
- [OpenIddict EF Core Integration](https://documentation.openiddict.com/integrations/entity-framework-core) — Shared DbContext pattern (HIGH confidence)
- [OpenIddict PKCE documentation](https://documentation.openiddict.com/configuration/proof-key-for-code-exchange) — RequireProofKeyForCodeExchange configuration (HIGH confidence)

---
*Architecture research for: SimpleAdmin — .NET 10 + OpenIddict + Vue 3 SPA user management POC*
*Researched: 2026-03-11*
