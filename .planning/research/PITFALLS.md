# Pitfalls Research

**Domain:** .NET 10 Web API + OpenIddict 6.x + Vue 3 SPA (Authorization Code flow, POC)
**Researched:** 2026-03-11
**Confidence:** HIGH (critical pitfalls verified against official docs and GitHub issues)

---

## Critical Pitfalls

### Pitfall 1: SPA Client Registered as Confidential Instead of Public

**What goes wrong:**
OpenIddict rejects all authorization requests from the Vue SPA with a `client_secret` validation error or forces the SPA to send a secret it cannot safely hold. The auth flow breaks immediately at the authorization endpoint.

**Why it happens:**
Developers copy server-to-server (client credentials) examples from docs or tutorials and register the SPA application with `ClientType = OpenIddictConstants.ClientTypes.Confidential`. Confidential clients require a secret. SPAs cannot hold a secret securely — it would be exposed in browser memory and network traffic.

**How to avoid:**
When seeding the OpenIddict application record for the Vue SPA, set `ClientType = OpenIddictConstants.ClientTypes.Public`. Combine with `RequireProofKeyForCodeExchange()` on the server and `code_challenge_method=S256` in client requests. Never register a `ClientSecret` for the SPA application descriptor.

```csharp
// Server options
options.AllowAuthorizationCodeFlow()
       .RequireProofKeyForCodeExchange();

// Application seed
await manager.CreateAsync(new OpenIddictApplicationDescriptor
{
    ClientId = "vue-spa",
    ClientType = OpenIddictConstants.ClientTypes.Public,
    RedirectUris = { new Uri("http://localhost:5173/callback") },
    Permissions = { ... }
});
```

**Warning signs:**
- Auth request returns `invalid_client` or a 400 demanding a `client_secret`.
- The SPA sends an empty or missing `client_secret` and gets rejected.

**Phase to address:** Authorization server setup (Phase 1 / backend foundation).

---

### Pitfall 2: Exact redirect_uri Mismatch Causes Persistent Authorization Failures

**What goes wrong:**
Every authorization request returns `invalid_redirect_uri` (OpenIddict error ID2043). The auth flow cannot complete. This is the single most frequently reported OpenIddict issue on GitHub.

**Why it happens:**
OpenIddict enforces exact string matching of the `redirect_uri` in the authorization request against the list registered in the application descriptor. Common mismatches:
- Trailing slash: `http://localhost:5173/callback/` vs `http://localhost:5173/callback`
- HTTP vs HTTPS: `https://localhost:5173` vs `http://localhost:5173`
- Port mismatch: Vite dev server defaults to `5173` but can shift to `5174` if port is occupied.
- Fragment vs path callback route.

**How to avoid:**
Register the exact URI as it appears in the browser address bar. During seeding, print the registered URIs to console to confirm. In the Vue client (oidc-client-ts), set `redirect_uri` to match exactly. Consider registering both HTTP and HTTPS localhost variants during development.

**Warning signs:**
- Browser console shows a redirect to the error page with `error=invalid_redirect_uri`.
- Vite console shows it switched to port 5174 (port 5173 was in use).
- Works in one browser session, fails after clearing state.

**Phase to address:** Authorization server setup (Phase 1); Vue OIDC client setup (Phase 2).

---

### Pitfall 3: Missing Explicit Application Permissions Break Every OAuth Request

**What goes wrong:**
Authorization or token requests fail with cryptic errors like `The client application is not allowed to use the specified grant type` or `The specified scope is not valid`. This happens even when the flow is configured on the server.

**Why it happens:**
OpenIddict enforces per-application permissions at four levels: endpoint access, grant type, scopes, and response types. Each must be explicitly granted on the application descriptor. The server-level `AllowAuthorizationCodeFlow()` call enables the flow globally, but individual client applications still need the matching permission (`Permissions.GrantTypes.AuthorizationCode`).

The `openid` and `offline_access` scopes are exempt, but all custom scopes and standard profile/email scopes require explicit `Permissions.Scopes.Profile`, `Permissions.Scopes.Email` entries.

**How to avoid:**
The application descriptor must include all of:
```csharp
Permissions =
{
    OpenIddictConstants.Permissions.Endpoints.Authorization,
    OpenIddictConstants.Permissions.Endpoints.Token,
    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
    OpenIddictConstants.Permissions.ResponseTypes.Code,
    OpenIddictConstants.Permissions.Scopes.Profile,
    OpenIddictConstants.Permissions.Scopes.Email,
    // any custom scope like "api" needs: Permissions.Prefixes.Scope + "api"
}
```

**Warning signs:**
- Token endpoint returns `unauthorized_client` or `invalid_scope`.
- Error message mentions "not allowed" for an endpoint or grant type.
- Adding a new scope breaks the flow unexpectedly.

**Phase to address:** Authorization server setup (Phase 1).

---

### Pitfall 4: OpenIddict Scope Registration Mismatch

**What goes wrong:**
Token requests fail with `invalid_scope` even when the application has the scope permission. This is a separate issue from application permissions.

**Why it happens:**
OpenIddict 6.x requires scopes to be explicitly registered on the server via `options.RegisterScopes(...)`. The application permission grants the client the right to request a scope; the server-level scope registration makes the scope valid to begin with. Developers grant the permission but forget to register the scope globally.

**How to avoid:**
```csharp
options.RegisterScopes(
    OpenIddictConstants.Scopes.OpenId,
    OpenIddictConstants.Scopes.Profile,
    OpenIddictConstants.Scopes.Email,
    "api" // custom scope for the resource API
);
```

**Warning signs:**
- `invalid_scope` returned at the token endpoint even with correct application permissions.
- The scope appears to work with `IgnoreScopePermissions()` workaround (which masks the real problem).

**Phase to address:** Authorization server setup (Phase 1).

---

### Pitfall 5: CORS Policy Blocks OIDC Token Requests from Vue SPA

**What goes wrong:**
The browser blocks the token endpoint request (`POST /connect/token`) with a CORS error. The authorization code exchange fails silently or with a network error. The auth code was obtained but cannot be exchanged for tokens.

**Why it happens:**
The Vue SPA runs on `http://localhost:5173` while the .NET API runs on `https://localhost:7xxx`. These are different origins. CORS must explicitly allow:
- The Vue origin.
- The `Authorization` and `Content-Type` request headers.
- Credentials if cookies are involved.

Developers often misconfigure CORS in one of these ways:
- Using `AllowAnyOrigin()` combined with `AllowCredentials()` — this is an invalid combination that ASP.NET Core rejects at startup.
- Forgetting to include `app.UseCors()` in the middleware pipeline, or placing it after `app.UseAuthentication()`.
- Hardcoding the origin but forgetting to update it when Vite uses a different port.
- Allowing origins but omitting `WithHeaders("Authorization", "Content-Type")`.

**How to avoid:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueSpa", policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Middleware order: CORS must come before UseAuthentication and UseAuthorization
app.UseCors("VueSpa");
app.UseAuthentication();
app.UseAuthorization();
```

**Warning signs:**
- Browser DevTools shows `Access-Control-Allow-Origin` missing in response headers.
- Preflight `OPTIONS` request returns 405.
- The auth code redirect works but the token exchange silently fails.
- Axios or fetch throws a network error (not an HTTP error) on the token request.

**Phase to address:** Backend infrastructure (Phase 1).

---

### Pitfall 6: CORS Middleware Ordering Breaks Authentication

**What goes wrong:**
CORS headers are not sent on `401 Unauthorized` or `403 Forbidden` responses. The browser sees a CORS error when the real problem is an expired or invalid token. Debugging is confusing because the error appears to be CORS when it is actually authentication.

**Why it happens:**
When `app.UseAuthentication()` is placed before `app.UseCors()`, authentication middleware processes the request first and short-circuits with a 401 response before CORS headers are added. The browser then sees a CORS failure rather than the actual auth error.

**How to avoid:**
Strictly follow this middleware order:
1. `app.UseRouting()`
2. `app.UseCors("VueSpa")`
3. `app.UseAuthentication()`
4. `app.UseAuthorization()`
5. `app.MapControllers()` / `app.MapRazorPages()`

**Warning signs:**
- 401 responses from the API do not include CORS headers.
- Browser shows CORS error but Postman shows 401 (no CORS involved in Postman).

**Phase to address:** Backend infrastructure (Phase 1).

---

### Pitfall 7: OpenIddict Validation Not Configured to Use Local Server

**What goes wrong:**
API controllers decorated with `[Authorize]` reject all tokens with 401, even valid ones freshly issued by the same application's OpenIddict server.

**Why it happens:**
When OpenIddict is both the server and the validator in the same application, validation must explicitly declare it is using the local server instance. Without `options.UseLocalServer()` in the validation configuration, OpenIddict does not know where to find signing keys and rejects all tokens.

Additionally, if `UseDataProtection()` is enabled on the server stack (which switches tokens from JWT to opaque Data Protection format), it must also be enabled in the validation options. Mismatched formats cause silent validation failures.

**How to avoid:**
```csharp
services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.UseLocalServer();     // import keys from local server instance
        options.UseAspNetCore();
        // if server uses UseDataProtection(), add it here too
    });
```

API controllers should use `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme` rather than `JwtBearer` as the scheme.

**Warning signs:**
- All API calls return 401 immediately after login.
- Tokens decode correctly in jwt.io but are rejected by the API.
- Removing `[Authorize]` makes the endpoint work.

**Phase to address:** Backend infrastructure + API endpoints (Phase 1 / Phase 2).

---

### Pitfall 8: Razor Login Page and Web API Coexisting — Antiforgery and Routing Conflicts

**What goes wrong:**
Several failure modes when mixing Razor Pages with a Web API project:
1. The login form POST returns 400 Bad Request due to missing antiforgery token — especially when using AJAX or non-standard form submissions.
2. Controller routes conflict with Razor Page routes when a route parameter named `page` is used.
3. `AddRazorPages()` is called but `MapRazorPages()` is not, so the login page returns 404.

**Why it happens:**
Web API project templates do not include Razor Pages by default. Adding them requires both a service registration (`AddRazorPages()`) and an endpoint mapping (`MapRazorPages()`). Antiforgery is automatically applied to Razor Page POST handlers; the `FormTagHelper` injects `__RequestVerificationToken` into forms, but this only works when the form is rendered by the tag helper — hand-crafted forms or JavaScript POSTs will be missing it.

The `page` parameter name is reserved by Razor Pages routing. If any Web API controller route uses `{page}` as a route segment, ASP.NET Core will produce unexpected routing behavior.

**How to avoid:**
- Add both `builder.Services.AddRazorPages()` and `app.MapRazorPages()`.
- Use the `<form>` tag helper with `method="post"` so antiforgery tokens are injected automatically.
- Avoid `{page}` as a route parameter name in controllers.
- Place `app.MapRazorPages()` after `app.MapControllers()` to give controller routes priority.
- Verify the login page is accessible at its expected URL before connecting the OIDC flow.

**Warning signs:**
- Login page 404 despite the `.cshtml` file existing.
- Login POST returns 400 without a body.
- Controller endpoints stop matching after adding Razor Pages.

**Phase to address:** Authorization server setup (Phase 1).

---

### Pitfall 9: In-Memory EF Database Does Not Persist Across Requests When DbContext is Scoped Incorrectly

**What goes wrong:**
Seeded OpenIddict applications or Microsoft Identity users disappear between requests, or are not visible across multiple DbContext instances. The authorization flow fails because OpenIddict cannot find the registered client application or user record.

**Why it happens:**
EF Core in-memory databases are namespaced by their service provider. When different parts of the application use different `DbContext` instances that each get their own internal service provider (due to different configuration options), they access different in-memory stores. The `IHostedService` that seeds data writes to a different store than the store OpenIddict reads at request time.

**How to avoid:**
Pass the same `InMemoryDatabaseRoot` instance to all `UseInMemoryDatabase()` calls:
```csharp
var inMemoryRoot = new InMemoryDatabaseRoot();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseInMemoryDatabase("SimpleAdmin", inMemoryRoot);
    options.UseOpenIddict();
});
```
Register the root as a singleton if needed in the seeding service.

Also note: in-memory databases do not support transactions. If any startup code uses `BeginTransaction()`, it will throw. Configure EF Core to suppress the `InMemoryEventId.TransactionIgnoredWarning` or avoid transactions entirely.

**Warning signs:**
- Login works on first boot but fails if the application restarts without re-seeding.
- OpenIddict throws `null reference` or "application not found" on auth requests.
- Seeding logs success but the flow still fails with unknown client.

**Phase to address:** Backend foundation / data layer (Phase 1).

---

### Pitfall 10: OpenAPI TypeScript Client Generation Fails or Produces Broken Types

**What goes wrong:**
The generated TypeScript client has incorrect types, missing endpoints, or runtime errors when calling authenticated endpoints. The Vue SPA cannot compile or the API calls fail with incorrect payloads.

**Why it happens:**
Several common causes:
- The `.NET` project uses `Microsoft.AspNetCore.OpenApi` which produces OpenAPI 3.1, but the chosen client generator (NSwag, openapi-ts) defaults to 3.0 parsing and mishandles `anyOf`/nullability changes between the versions.
- The OpenAPI spec is generated at build time but the API is not running when the generator runs, producing an empty or stale spec.
- Swagger/OpenAPI annotations are missing on controllers, causing endpoints to not appear in the spec.
- Bearer security scheme is defined in the spec but the generated client does not automatically attach the `Authorization` header — it must be wired up manually via interceptors.

**How to avoid:**
- Explicitly choose between `NSwag` and `@hey-api/openapi-ts`. For .NET 10 with `Microsoft.AspNetCore.OpenApi`, prefer `@hey-api/openapi-ts` which handles OpenAPI 3.1 correctly.
- Generate the spec from the running API or use a build-time document generation step (NSwag MSBuild task or `dotnet openapi`).
- Add a Vite npm script that calls the generator and pipe it into the build process.
- After generation, verify the client wires the bearer token via an interceptor:
  ```typescript
  client.interceptors.request.use((request) => {
    const token = authStore.accessToken;
    if (token) request.headers.set('Authorization', `Bearer ${token}`);
    return request;
  });
  ```

**Warning signs:**
- Generated client file is empty or contains only type stubs.
- TypeScript compilation errors after generation.
- API calls succeed in Postman but fail from the Vue app with 401.
- Null/undefined errors at runtime on fields that should always be present.

**Phase to address:** API contract + frontend client setup (Phase 2 / Phase 3).

---

### Pitfall 11: oidc-client-ts State Management on Page Refresh Loses Authentication

**What goes wrong:**
After a successful login, refreshing the Vue SPA page loses the authenticated state. The user is shown the login screen again despite having a valid session.

**Why it happens:**
`oidc-client-ts` with its default in-memory `UserManager` storage does not persist the user object across page reloads. The auth state exists only in JavaScript memory. On refresh, the `UserManager` is reinitialized empty.

If `sessionStorage` or `localStorage` is used instead, the token persists but becomes an XSS risk. For a POC, `sessionStorage` is an acceptable tradeoff.

**How to avoid:**
For this POC, configure `UserManager` with `userStore: new WebStorageStateStore({ store: window.sessionStorage })`. This persists state for the tab session without requiring silent refresh or a dedicated auth proxy. Document this as a POC-only decision — production would need either a BFF pattern or in-memory storage with silent refresh.

**Warning signs:**
- After redirect back from login, the SPA appears authenticated, but `F5` shows the login page again.
- The token endpoint was called but `getUser()` returns `null`.
- Vue Router guards redirect to login on every hard refresh.

**Phase to address:** Vue OIDC client setup (Phase 2).

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| In-memory EF database | No migration setup needed | Data lost on restart; no concurrency support; transactions silently ignored | POC only — document clearly |
| `AllowAnyHeader()` / `AllowAnyMethod()` in CORS | No need to enumerate headers | Overly permissive for production | POC only — tighten before any real deployment |
| `sessionStorage` for OIDC tokens | Simple persistence without BFF | XSS exposes tokens; not viable in production | POC only — use BFF or silent refresh in production |
| `DisableTokenStorage` in OpenIddict | Removes need for tokens table in EF | No token revocation; replay attacks possible | POC where revocation is out of scope |
| Hardcoded `localhost:5173` in CORS policy | Fast to set up | Breaks when port changes or in CI | Acceptable if read from `appsettings.Development.json` |
| `IgnoreEndpointPermissions()` during dev | Faster iteration | Masks missing permission config that breaks in prod | Never — always configure permissions correctly from the start |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| OpenIddict + Microsoft Identity | Calling `AddDefaultIdentity` which registers the default UI and cookies that conflict with OpenIddict's flow | Use `AddIdentityCore` or `AddIdentity` without the default UI; only add cookie authentication that OpenIddict needs |
| OpenIddict + EF Core in-memory | Forgetting `options.UseOpenIddict()` on the DbContext | Always call `UseOpenIddict()` on the DbContext options alongside `UseInMemoryDatabase` |
| Vue + OpenIddict | Using the implicit flow (no longer recommended) instead of auth code + PKCE | Always use `AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange()` |
| .NET API + Razor Pages | Adding `AddRazorPages()` but not `MapRazorPages()` | Both are required; Map call must be in the endpoint routing section |
| Vite dev server + .NET API | Using `fetch` with a relative URL assumes same origin | Use the absolute URL of the .NET API, or configure Vite proxy to forward non-OIDC API calls |
| OpenAPI client + bearer token | Generated client does not know where to get the token | Manually wire the interceptor to read from the auth store (Pinia) after generation |
| `AddAuthentication` + OpenIddict validation | Setting default scheme to `JwtBearer` when OpenIddict uses its own scheme | Set default scheme to `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme` |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Seeding data in `IHostedService` without awaiting before first request | Race condition: first request arrives before seed completes | Use `IHostApplicationLifetime.ApplicationStarted` or seed synchronously in `Program.cs` | Under any load; reproducible in dev with fast startup |
| EF in-memory with large datasets | Slow queries; no index support | N/A for POC; use real DB for performance testing | At ~1,000 entities for complex queries |
| Token validation on every request fetching JWKS | Repeated key fetches slow API | `UseLocalServer()` caches keys in-process; no external fetch needed | N/A when using `UseLocalServer()` |

*(Performance traps are largely irrelevant for this POC — noted for awareness only.)*

---

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Registering SPA as confidential client with a secret | Secret exposed in browser; client impersonation | Always use `ClientTypes.Public` + PKCE for SPA clients |
| Storing access tokens in `localStorage` | XSS attack steals tokens permanently | Use `sessionStorage` (POC) or in-memory + silent refresh (production) |
| `AllowAnyOrigin()` + `AllowCredentials()` | ASP.NET Core throws at startup; if somehow bypassed, CSRF risk | Never combine; use explicit origin allowlist |
| Disabling antiforgery on the Razor login page | CSRF attacks on the login endpoint | Keep antiforgery on Razor Pages; ensure form uses tag helpers |
| Not enforcing HTTPS on the token endpoint | Tokens transmitted in plaintext | Even in dev, use `https://localhost` for the API; trust the dev cert |
| Leaving `IgnoreEndpointPermissions()` in code | Any client can hit any endpoint regardless of registration | Remove before any shared or deployed environment |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| No redirect after login — SPA stays on callback URL | User sees a raw JSON or blank callback page after auth | Handle the OIDC callback route in Vue Router; redirect to `/` or intended page after `signinRedirectCallback()` |
| Login page submits and hangs with no error on bad credentials | User confused; no feedback | Add model validation and an error message to the Razor login page |
| Token expiry causes silent 401 in Vue app | CRUD operations fail without explanation | Check `user.expired` before API calls; show re-login prompt on 401 response |
| Authorization code redirect loop | User bounces between login and SPA indefinitely | Guard OIDC callback route from triggering another auth check; check if user is already authenticated before redirecting |

---

## "Looks Done But Isn't" Checklist

- [ ] **OpenIddict server:** `AllowAuthorizationCodeFlow()` and `RequireProofKeyForCodeExchange()` are both called — not just one.
- [ ] **Application descriptor:** All four permission categories set (endpoint, grant type, scope, response type) — not just grant type.
- [ ] **CORS:** `app.UseCors()` is placed before `app.UseAuthentication()` in the pipeline.
- [ ] **Razor login page:** `MapRazorPages()` is in the endpoint routing section — not just `AddRazorPages()` in services.
- [ ] **Validation stack:** `options.UseLocalServer()` is called in `AddValidation()` — otherwise all tokens are rejected.
- [ ] **Vue OIDC client:** The callback route in Vue Router calls `signinRedirectCallback()` and then redirects away from the callback URL.
- [ ] **OpenAPI client:** Bearer token interceptor is wired to the auth store — generated client does not attach tokens automatically.
- [ ] **In-memory seed:** Seeded data (OpenIddict apps, Identity users) is confirmed to be visible to request-handling DbContext instances.
- [ ] **Scope registration:** Custom scopes are registered via `options.RegisterScopes()` on the server — not only granted in the app descriptor.
- [ ] **redirect_uri:** The exact URI registered in the OpenIddict app descriptor matches what `oidc-client-ts` sends — including port, scheme, and trailing slash.

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Wrong client type (confidential vs public) | LOW | Update the seeding code; restart the app (in-memory DB resets, so no migration needed) |
| Redirect URI mismatch | LOW | Update the URI in the seeding code and/or the Vue `UserManager` config |
| Missing permissions | LOW | Add permissions to the app descriptor in the seeding code; restart |
| CORS misconfiguration | LOW | Adjust `AddCors` policy; fix middleware order |
| Validation not using local server | LOW | Add `options.UseLocalServer()` to validation config |
| In-memory data isolation | MEDIUM | Introduce `InMemoryDatabaseRoot` singleton; may require refactoring startup code |
| OpenAPI client wrong generator/version | MEDIUM | Switch generator tool; re-run generation; manually fix broken type usages |
| OIDC session lost on refresh | LOW | Switch `UserManager` to `sessionStorage` store |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| SPA registered as confidential | Phase 1: Auth server setup | Auth code request completes without client_secret error |
| redirect_uri mismatch | Phase 1 (register) + Phase 2 (Vue config) | Full auth code flow completes end-to-end |
| Missing application permissions | Phase 1: Auth server setup | Token endpoint returns tokens for all required scopes |
| Scope not registered | Phase 1: Auth server setup | `openid profile email` scopes work in token response |
| CORS blocking token requests | Phase 1: Backend infrastructure | Browser network tab shows 200 on `/connect/token` |
| CORS middleware ordering | Phase 1: Backend infrastructure | 401 responses still include `Access-Control-Allow-Origin` header |
| Validation not using local server | Phase 1: Backend infrastructure | `[Authorize]` controller returns 200 with valid token |
| Razor Pages routing/antiforgery | Phase 1: Auth server setup | Login page renders and POST succeeds |
| In-memory DB data isolation | Phase 1: Data layer | Auth flow succeeds on second request without restart |
| OpenAPI client generation | Phase 2/3: Frontend client setup | TypeScript compiles; API calls succeed with token |
| OIDC session on page refresh | Phase 2: Vue OIDC setup | Hard refresh keeps user authenticated within tab session |

---

## Sources

- [OpenIddict Application Permissions](https://documentation.openiddict.com/configuration/application-permissions)
- [OpenIddict Creating Your Own Server Instance](https://documentation.openiddict.com/guides/getting-started/creating-your-own-server-instance)
- [OpenIddict Implementing Token Validation in APIs](https://documentation.openiddict.com/guides/getting-started/implementing-token-validation-in-your-apis)
- [OpenIddict ASP.NET Core Data Protection Integration](https://documentation.openiddict.com/integrations/aspnet-core-data-protection)
- [OpenIddict Migration Guide 5.0 to 6.0](https://documentation.openiddict.com/guides/migration/50-to-60)
- [GitHub: Redirect URI issue in OpenIddict #1554](https://github.com/openiddict/openiddict-core/issues/1554)
- [GitHub: Authorization Code Grant with PKCE #706](https://github.com/openiddict/openiddict-core/issues/706)
- [GitHub: OpenIddict with SQLite in-memory for integration tests #684](https://github.com/openiddict/openiddict-core/issues/684)
- [GitHub: OpenIddict integrated into vanilla .NET 8 Identity Web UI #321](https://github.com/openiddict/openiddict-samples/issues/321)
- [Can you use ASP.NET Core Identity API endpoints with OpenIddict? — Kévin Chalet](https://kevinchalet.com/2023/10/04/can-you-use-the-asp-net-core-identity-api-endpoints-with-openiddict/)
- [Setting up OpenIddict Part IV: Authorization Code Flow — DEV Community](https://dev.to/robinvanderknaap/setting-up-an-authorization-server-with-openiddict-part-iv-authorization-code-flow-3eh8)
- [EF Core In-Memory Provider Limitations — João Grassi](https://blog.joaograssi.com/limitations-ef-core-in-memory-database-providers/)
- [EF Core In-Memory Database Provider — Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/providers/in-memory/)
- [Avoid In-Memory Databases for Tests — Jimmy Bogard](https://www.jimmybogard.com/avoid-in-memory-databases-for-tests/)
- [ASP.NET Core and CORS Gotchas — Rick Strahl](https://weblog.west-wind.com/posts/2016/sep/26/aspnet-core-and-cors-gotchas)
- [Hey API openapi-ts Middleware & Auth](https://openapi-ts.dev/openapi-fetch/middleware-auth)
- [Full-stack static typing with OpenAPI TypeScript — johnnyreilly](https://johnnyreilly.com/dotnet-openapi-and-openapi-ts)
- [The Struggle with Vue and ASP.NET Core — Don't Panic Labs](https://dontpaniclabs.com/blog/post/2025/02/25/the-struggle-with-vue-and-asp-net-core/)
- [Securing a Vue.js app using OpenID Connect Code Flow with PKCE — damienbod](https://damienbod.com/2019/01/29/securing-a-vue-js-app-using-openid-connect-code-flow-with-pkce-and-identityserver4/)

---
*Pitfalls research for: .NET 10 + OpenIddict 6.x + Vue 3 SPA (Authorization Code + PKCE, POC)*
*Researched: 2026-03-11*
