# Phase 1: Backend Foundation - Research

**Researched:** 2026-03-11
**Domain:** .NET 10 / OpenIddict 7.x / ASP.NET Core Identity / EF Core In-Memory
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **Login page appearance:** Minimal centered form — clean white card centered on the page, no framework CSS dependency (no Bootstrap, no PrimeVue CSS). "SimpleAdmin" title as a heading above the form, no logo or tagline. Email + Password fields only — no "remember me" checkbox, no "forgot password" link. Single red/orange error banner above the form for invalid credentials ("Invalid email or password"). Submit button labeled "Sign In".
- **Identity configuration:** Use `AddIdentity<ApplicationUser, IdentityRole>` (not `AddDefaultIdentity`) to avoid default UI conflicts with OpenIddict. Pass shared `InMemoryDatabaseRoot` singleton to all `UseInMemoryDatabase()` calls to avoid data isolation across DbContext instances.
- **Auth scheme:** Use `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme` as default auth scheme, not JwtBearer.

### Claude's Discretion

- Project structure and solution layout
- OpenIddict client seed configuration (redirect URIs, scopes, token lifetimes)
- Which protected test endpoint to scaffold for Bearer token validation
- Razor page layout/CSS implementation details beyond the decisions above

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| AUTH-01 | User can log in via Authorization Code + PKCE flow through a Razor-rendered login page | OpenIddict 7.3 Authorization Code flow with `EnableAuthorizationEndpointPassthrough()`, Razor login page using `HttpContext.SignInAsync` with cookie scheme, then redirect back to authorize endpoint |
| AUTH-03 | All API requests include a Bearer token; unauthenticated requests return 401 | `AddValidation` with `UseLocalServer()` + `UseAspNetCore()`, `AddAuthentication` defaulting to `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme`, `[Authorize]` attribute on protected controller/endpoint |
</phase_requirements>

---

## Summary

This phase bootstraps a single ASP.NET Core (.NET 10) project that acts as both an OpenIddict authorization server and a protected resource server. OpenIddict 7.3 (released March 9, 2026) natively targets .NET 10.0 and is the prescribed library for OAuth 2.0 / OpenID Connect. The server exposes `/connect/authorize` and `/connect/token` in passthrough mode, meaning a custom `AuthorizationController` and Razor login page handle user interaction while OpenIddict validates requests and issues tokens.

EF Core's in-memory provider is used for all storage. A shared `InMemoryDatabaseRoot` singleton must be registered and passed to every `UseInMemoryDatabase()` call to ensure Identity tables and OpenIddict tables live in the same logical database regardless of how service scopes are resolved. Development encryption/signing certificates (`AddDevelopmentEncryptionCertificate()` / `AddDevelopmentSigningCertificate()`) are sufficient for this POC; `DisableAccessTokenEncryption()` is recommended for local SPA interop so the Vue client and curl can read the JWT without decryption keys.

Token validation for protected API endpoints is configured via `AddOpenIddict().AddValidation(options => { options.UseLocalServer(); options.UseAspNetCore(); })`, and the default authentication scheme is set to `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme`. A seeder `IHostedService` (commonly called `OpenIddictWorker`) runs at startup to register the SPA client application with all required permissions before any requests arrive.

**Primary recommendation:** Single .NET 10 `dotnet new webapi` project with Razor Pages added. Register Identity, OpenIddict server+validation, EF Core in-memory with shared root, and a hosted seeder in Program.cs. Keep the Authorize controller, login Razor page, and a protected health/me endpoint as the three moving pieces.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| OpenIddict.AspNetCore | 7.3.0 | OAuth 2.0 / OIDC server + validation middleware | Official OpenIddict ASP.NET Core integration; .NET 10 native |
| OpenIddict.EntityFrameworkCore | 7.3.0 | Persists applications, authorizations, scopes, tokens | Required store for OpenIddict core |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.x (ships with .NET 10 SDK) | User + role management, password hashing | Prescribed by locked decision |
| Microsoft.EntityFrameworkCore.InMemory | 10.x (ships with .NET 10 SDK) | In-memory relational store for Identity + OpenIddict | Stated constraint; no persistent DB for POC |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.AspNetCore.Authentication.Cookies | 10.x (built-in) | Cookie auth for the login Razor page session | Required to sign user in after credential validation before redirecting back to authorize endpoint |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| OpenIddict | Duende IdentityServer | IdentityServer has per-deployment licensing fees; OpenIddict is MIT |
| OpenIddict | Microsoft.Identity.Web + Azure AD | Requires external identity provider; not self-contained |
| EF Core In-Memory | SQLite In-Memory | SQLite supports FK constraints but adds complexity; not required for this POC |

**Installation:**
```bash
dotnet new webapi -n SimpleAdmin.Api --no-openapi
dotnet add package OpenIddict.AspNetCore --version 7.3.0
dotnet add package OpenIddict.EntityFrameworkCore --version 7.3.0
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

---

## Architecture Patterns

### Recommended Project Structure

```
SimpleAdmin.Api/
├── Controllers/
│   ├── AuthorizationController.cs    # Passthrough: /connect/authorize, /connect/token
│   └── ApiController.cs              # Protected test endpoint (e.g. GET /api/me)
├── Data/
│   └── ApplicationDbContext.cs       # IdentityDbContext<ApplicationUser> + UseOpenIddict()
├── Models/
│   └── ApplicationUser.cs            # Extends IdentityUser (can be minimal)
├── Pages/
│   └── Account/
│       ├── Login.cshtml              # Razor login page
│       └── Login.cshtml.cs           # PageModel: OnGet, OnPost
├── Workers/
│   └── OpenIddictWorker.cs           # IHostedService: seeds client application
└── Program.cs                        # All service + middleware registration
```

### Pattern 1: Shared InMemoryDatabaseRoot Singleton

**What:** A single `InMemoryDatabaseRoot` instance registered as a singleton and passed to every `UseInMemoryDatabase()` call. This is required because OpenIddict and Identity each resolve a scoped `ApplicationDbContext`; without a shared root, different service provider scopes get isolated databases.

**When to use:** Any time two or more `DbContext` instances (or scopes) must share the same in-memory data.

**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.inmemorydbcontextoptionsextensions.useinmemorydatabase
// Register once in Program.cs
var inMemoryRoot = new InMemoryDatabaseRoot();
builder.Services.AddSingleton(inMemoryRoot);

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var root = sp.GetRequiredService<InMemoryDatabaseRoot>();
    options.UseInMemoryDatabase("SimpleAdmin", root);
    options.UseOpenIddict();
});
```

### Pattern 2: Identity + OpenIddict Service Registration

**What:** Register `AddIdentity` (not `AddDefaultIdentity`) then chain OpenIddict onto the same `DbContext`.

**When to use:** Every time Identity and OpenIddict share a single project.

**Example:**
```csharp
// Source: https://documentation.openiddict.com/guides/getting-started/creating-your-own-server-instance
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token");

        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.DisableAccessTokenEncryption(); // readable JWTs for SPA + curl

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement(); // allow HTTP in dev
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});
builder.Services.AddAuthorization();
```

### Pattern 3: Authorization Controller (Passthrough Mode)

**What:** An MVC controller that handles `/connect/authorize` (GET+POST) and `/connect/token`. On the authorize endpoint, if the user is not cookie-authenticated, it issues a `Challenge` redirect to the login page. After login, it builds a `ClaimsIdentity` and calls `SignIn` with `OpenIddictServerAspNetCoreDefaults.AuthenticationScheme`.

**When to use:** Required for every Authorization Code flow setup in OpenIddict.

**Example:**
```csharp
// Source: https://dev.to/robinvanderknaap/setting-up-an-authorization-server-with-openiddict-part-iv-authorization-code-flow-3eh8
[ApiController]
public class AuthorizationController : ControllerBase
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict request not found.");

        var result = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
        {
            return Challenge(
                authenticationSchemes: CookieAuthenticationDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path +
                                  QueryString.Create(Request.HasFormContentType
                                      ? Request.Form.ToList()
                                      : Request.Query.ToList())
                });
        }

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, result.Principal.FindFirstValue(ClaimTypes.NameIdentifier))
                .SetClaim(Claims.Email, result.Principal.FindFirstValue(ClaimTypes.Email))
                .SetClaim(Claims.Name, result.Principal.FindFirstValue(ClaimTypes.Name));

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict request not found.");

        if (request.IsAuthorizationCodeGrantType())
        {
            var principal = (await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

            return SignIn(principal!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("Unsupported grant type.");
    }
}
```

### Pattern 4: OpenIddictWorker (Hosted Seeder)

**What:** An `IHostedService` that runs once at startup to register the SPA client application in the OpenIddict `applications` table.

**When to use:** Always — the in-memory database is empty on every restart.

**Example:**
```csharp
// Source: https://documentation.openiddict.com/guides/getting-started/creating-your-own-server-instance
public class OpenIddictWorker : IHostedService
{
    private readonly IServiceProvider _provider;
    public OpenIddictWorker(IServiceProvider provider) => _provider = provider;

    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = _provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync(ct);

        // Seed admin user
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminEmail = "admin@simpleadmin.local";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var user = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            await userManager.CreateAsync(user, "Admin1234!");
        }

        // Register SPA client
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        const string clientId = "simple-admin-spa";
        if (await manager.FindByClientIdAsync(clientId, ct) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientType = ClientTypes.Public,
                RedirectUris = { new Uri("http://localhost:5173/callback") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Prefixes.Scope + "api"
                }
            }, ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

### Pattern 5: Razor Login Page (PageModel)

**What:** `/Pages/Account/Login.cshtml` + `Login.cshtml.cs`. On `OnPostAsync`, uses `SignInManager` to validate credentials, then calls `HttpContext.SignInAsync` with cookie scheme and redirects back to `ReturnUrl`.

**When to use:** The locked decision requires a Razor page (not MVC view) for the login UI.

**Example:**
```csharp
// Pages/Account/Login.cshtml.cs
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    public LoginModel(SignInManager<ApplicationUser> signInManager)
        => _signInManager = signInManager;

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var result = await _signInManager.PasswordSignInAsync(
            Email, Password, isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ErrorMessage = "Invalid email or password";
            ReturnUrl = returnUrl;
            return Page();
        }

        return Redirect(returnUrl ?? "/");
    }
}
```

### Anti-Patterns to Avoid

- **Using `AddDefaultIdentity` instead of `AddIdentity`:** `AddDefaultIdentity` scaffolds built-in account controllers that conflict with OpenIddict's passthrough endpoints and override the cookie scheme in unexpected ways. Always use `AddIdentity<ApplicationUser, IdentityRole>`.
- **No shared `InMemoryDatabaseRoot`:** Without a shared root, the seeder worker resolves a DbContext from a different service scope than the one Identity uses at request time — data written in `StartAsync` is invisible to incoming requests. Register the root as a singleton.
- **Setting `DefaultScheme` to cookie only:** If the default scheme is `CookieAuthenticationDefaults.AuthenticationScheme`, Bearer token requests to API endpoints will attempt cookie authentication, fail silently, and return a redirect rather than 401. The default scheme must be `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme`; cookie auth should be set as `DefaultChallengeScheme` or applied per-endpoint.
- **Forgetting `DisableTransportSecurityRequirement` in dev:** OpenIddict rejects HTTP (non-TLS) requests by default. During local development without HTTPS, call `DisableTransportSecurityRequirement()` on the ASP.NET Core host options.
- **Missing `UseOpenIddict()` on DbContext options:** The call `options.UseOpenIddict()` inside `AddDbContext` registers the OpenIddict entity model in the EF schema. Without it, `EnsureCreatedAsync` creates Identity tables but not the OpenIddict `applications`, `tokens`, etc., tables.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| OAuth 2.0 token issuance | Custom JWT generation | OpenIddict server | PKCE, code challenge verification, token introspection, refresh token rotation — massive surface area |
| Authorization code exchange | Custom code storage | OpenIddict EF Core stores | Codes must be single-use; replay attacks, expiry, revocation already handled |
| Password hashing | Custom bcrypt wrapper | `PasswordHasher<T>` (Identity) | NIST PBKDF2, salt generation, upgrade path on hash iteration count |
| PKCE verification | Custom code_verifier/challenge check | OpenIddict built-in | Exact SHA-256 base64url comparison; easy to get wrong |
| JWT signing/verification | Custom RSA/HMAC wiring | OpenIddict signing credentials | Algorithm negotiation, key rotation, JWKS endpoint all included |

**Key insight:** OAuth 2.0 has dozens of security-critical edge cases (token reuse, CSRF on redirect, code injection). OpenIddict encodes all of these in its validation pipeline. Writing any part of this manually introduces vulnerabilities that are invisible until exploited.

---

## Common Pitfalls

### Pitfall 1: redirect_uri Mismatch

**What goes wrong:** The token exchange at `/connect/token` returns `invalid_grant` or the authorize endpoint returns `redirect_uri_mismatch`.
**Why it happens:** The `redirect_uri` sent in the authorization request must exactly match (case-sensitive, including trailing slash) one of the URIs registered in `OpenIddictApplicationDescriptor.RedirectUris`.
**How to avoid:** Seed the worker with the exact URI the SPA will use (e.g., `http://localhost:5173/callback`). When testing with curl, pass `--data "redirect_uri=http://localhost:5173/callback"` in the token exchange.
**Warning signs:** `error=invalid_grant` in the token response JSON.

### Pitfall 2: Missing Permissions on the Seeded Client

**What goes wrong:** The authorize endpoint returns `access_denied` or `unauthorized_client` even with valid credentials.
**Why it happens:** OpenIddict validates that the client has explicit permission for every endpoint, grant type, and response type it uses. A missing `Permissions.Endpoints.Authorization` or `Permissions.GrantTypes.AuthorizationCode` causes the request to be rejected.
**How to avoid:** The seeded descriptor must include: `Permissions.Endpoints.Authorization`, `Permissions.Endpoints.Token`, `Permissions.GrantTypes.AuthorizationCode`, `Permissions.ResponseTypes.Code`, plus any scope permissions for scopes requested.
**Warning signs:** OpenIddict logs "The client application is not allowed to use the specified endpoint/grant type."

### Pitfall 3: 401 on Protected Endpoint Despite Valid Token

**What goes wrong:** GET `/api/me` with `Authorization: Bearer <token>` returns 401.
**Why it happens:** Most commonly one of: (a) default auth scheme is still cookie, so OpenIddict validation is never invoked; (b) `DisableAccessTokenEncryption()` was not called but the token was issued encrypted and the validator cannot read it without the encryption key; (c) `UseLocalServer()` was omitted from the validation block.
**How to avoid:** Verify `options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme` is set. Ensure `DisableAccessTokenEncryption()` is called both on the server and that no encryption key mismatch exists. Confirm `AddValidation(o => { o.UseLocalServer(); o.UseAspNetCore(); })` is present.
**Warning signs:** Token decodes correctly at jwt.io but API still returns 401; Identity-related 302 redirects instead of 401.

### Pitfall 4: Data Isolation Between Service Scopes (Missing Shared Root)

**What goes wrong:** Seeded users and client registrations are not visible during request handling — login always fails, or authorize endpoint reports client not found.
**Why it happens:** `UseInMemoryDatabase("name")` without a shared `InMemoryDatabaseRoot` roots the database in the service provider's internal container. The worker's scope and the request-handling scopes are children of the same root provider, but the in-memory store is isolated per service provider subtree without the explicit root.
**How to avoid:** Register `new InMemoryDatabaseRoot()` as a singleton before `AddDbContext`, and pass it as the second argument to `UseInMemoryDatabase`.
**Warning signs:** Login page always shows "Invalid email or password" even with the seeded admin credentials.

### Pitfall 5: Middleware Order

**What goes wrong:** `UseAuthentication` / `UseAuthorization` have no effect; all requests reach protected endpoints unauthenticated.
**Why it happens:** ASP.NET Core middleware order is strict. Auth middleware must come after `UseRouting` and before `UseEndpoints` / `MapControllers`.
**How to avoid:**
```
app.UseRouting();
app.UseCors();
app.UseAuthentication();   // must be before UseAuthorization
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
```
**Warning signs:** `[Authorize]` attribute is ignored; endpoints return 200 for unauthenticated requests.

### Pitfall 6: AddRazorPages Not Added When Using webapi Template

**What goes wrong:** Razor login page returns 404.
**Why it happens:** `dotnet new webapi` does not include Razor Pages by default.
**How to avoid:** Add `builder.Services.AddRazorPages()` and `app.MapRazorPages()` in Program.cs.
**Warning signs:** `/Account/Login` returns 404 regardless of the .cshtml file being present.

---

## Code Examples

### Complete Program.cs Registration Order

```csharp
// Source: https://documentation.openiddict.com/guides/getting-started/creating-your-own-server-instance
//         https://documentation.openiddict.com/guides/getting-started/implementing-token-validation-in-your-apis

var inMemoryRoot = new InMemoryDatabaseRoot();
builder.Services.AddSingleton(inMemoryRoot);

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseInMemoryDatabase("SimpleAdmin", sp.GetRequiredService<InMemoryDatabaseRoot>());
    options.UseOpenIddict();
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>())
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token");
        options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
        options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        options.DisableAccessTokenEncryption();
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
});

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHostedService<OpenIddictWorker>();
```

### ApplicationDbContext

```csharp
// IdentityDbContext already includes Identity tables; UseOpenIddict() adds OpenIddict tables
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
}
```

### Protected Endpoint (GET /api/me)

```csharp
// Source: https://documentation.openiddict.com/guides/getting-started/implementing-token-validation-in-your-apis
[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public IActionResult Me()
    {
        var sub = User.FindFirstValue(Claims.Subject);
        var email = User.FindFirstValue(Claims.Email);
        return Ok(new { sub, email });
    }
}
```

### Razor Login Page (minimal CSS — no framework)

```html
<!-- Pages/Account/Login.cshtml -->
@page
@model LoginModel
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>SimpleAdmin — Sign In</title>
    <style>
        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: system-ui, sans-serif; background: #f3f4f6;
               display: flex; align-items: center; justify-content: center; min-height: 100vh; }
        .card { background: #fff; border-radius: 8px; padding: 2rem; width: 360px;
                box-shadow: 0 2px 12px rgba(0,0,0,.1); }
        h1 { font-size: 1.4rem; margin-bottom: 1.5rem; text-align: center; }
        .error { background: #fff0f0; border: 1px solid #f87171; color: #b91c1c;
                 border-radius: 4px; padding: .6rem .8rem; margin-bottom: 1rem; font-size: .9rem; }
        label { display: block; font-size: .85rem; font-weight: 600; margin-bottom: .3rem; }
        input { width: 100%; border: 1px solid #d1d5db; border-radius: 4px;
                padding: .5rem .75rem; font-size: 1rem; margin-bottom: 1rem; }
        button { width: 100%; background: #2563eb; color: #fff; border: none;
                 border-radius: 4px; padding: .65rem; font-size: 1rem; cursor: pointer; }
        button:hover { background: #1d4ed8; }
    </style>
</head>
<body>
<div class="card">
    <h1>SimpleAdmin</h1>
    @if (Model.ErrorMessage is not null)
    {
        <div class="error">@Model.ErrorMessage</div>
    }
    <form method="post">
        <input type="hidden" asp-for="ReturnUrl" />
        <label asp-for="Email">Email</label>
        <input asp-for="Email" type="email" autocomplete="username" />
        <label asp-for="Password">Password</label>
        <input asp-for="Password" type="password" autocomplete="current-password" />
        <button type="submit">Sign In</button>
    </form>
</div>
</body>
</html>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| IdentityServer4 (free) | OpenIddict (MIT) | IdentityServer4 EOL 2022; Duende licensing 2022 | OpenIddict is the community standard for self-hosted OIDC in .NET |
| JwtBearer scheme for API validation | `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme` | OpenIddict 3.x | Handles encrypted tokens, introspection, and local-server key sharing automatically |
| Separate `Startup.cs` | Minimal hosting model (`Program.cs` only) | .NET 6+ | All registration in Program.cs; no `Configure`/`ConfigureServices` split |
| `AddDefaultIdentity` | `AddIdentity<TUser, TRole>` for OpenIddict projects | Always — documented as recommended | Avoids built-in UI page conflicts with custom OIDC endpoints |
| Implicit flow for SPAs | Authorization Code + PKCE | OAuth 2.0 Security BCP (2019) | Implicit flow deprecated; access tokens never exposed in fragment |

**Deprecated/outdated:**
- `UseInMemoryDatabase()` without a name argument: Marked `[Obsolete]` since EF Core 3. Always pass a string name and optionally an `InMemoryDatabaseRoot`.
- `DisableTransportSecurityRequirement()`: Still valid for local dev; do NOT carry this to production.
- Implicit flow: Removed from OpenIddict server options by default — do not attempt to use it.

---

## Open Questions

1. **CORS configuration for Phase 3**
   - What we know: Phase 3 will add a Vue SPA on `http://localhost:5173` that calls `/connect/authorize` and `/api/*`.
   - What's unclear: Whether CORS headers are needed at the OpenIddict authorize endpoint (browser navigates there, so likely no CORS needed) vs. the token endpoint (called via `fetch`, so CORS headers required).
   - Recommendation: Add a permissive CORS policy for `http://localhost:5173` in Phase 1 to avoid a re-visit; target `AllowAnyHeader`, `AllowAnyMethod`, `WithOrigins("http://localhost:5173")` on the token endpoint and API routes.

2. **Seeded admin credential management**
   - What we know: The seeder hardcodes `admin@simpleadmin.local / Admin1234!` for local dev.
   - What's unclear: Whether Phase 2 needs the seeder to create additional test users for user management scenarios.
   - Recommendation: Keep a single seeded admin for Phase 1; Phase 2 can extend the seeder.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | None detected — greenfield project; use xUnit 2.9+ with `Microsoft.AspNetCore.Mvc.Testing` |
| Config file | `SimpleAdmin.Tests/SimpleAdmin.Tests.csproj` — Wave 0 creation |
| Quick run command | `dotnet test --filter "Category=Smoke" --no-build` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| AUTH-01 | Authorize endpoint redirects unauthenticated user to login page | Integration (smoke) | `dotnet test --filter "FullyQualifiedName~AuthFlowSmokeTests"` | Wave 0 |
| AUTH-01 | POST login with valid credentials sets cookie and redirects back with code | Integration | `dotnet test --filter "FullyQualifiedName~AuthFlowSmokeTests"` | Wave 0 |
| AUTH-01 | POST `/connect/token` with valid code returns access token JSON | Integration | `dotnet test --filter "FullyQualifiedName~TokenEndpointTests"` | Wave 0 |
| AUTH-03 | GET `/api/me` with valid Bearer token returns 200 | Integration | `dotnet test --filter "FullyQualifiedName~ProtectedEndpointTests"` | Wave 0 |
| AUTH-03 | GET `/api/me` without token returns 401 | Integration | `dotnet test --filter "FullyQualifiedName~ProtectedEndpointTests"` | Wave 0 |

### Sampling Rate

- **Per task commit:** `dotnet test --filter "Category=Smoke"` (authorize redirect + 401 check)
- **Per wave merge:** `dotnet test` (full suite)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `SimpleAdmin.Tests/SimpleAdmin.Tests.csproj` — xUnit + `Microsoft.AspNetCore.Mvc.Testing` project
- [ ] `SimpleAdmin.Tests/AuthFlowSmokeTests.cs` — covers AUTH-01 redirect and code exchange
- [ ] `SimpleAdmin.Tests/ProtectedEndpointTests.cs` — covers AUTH-03 200/401 scenarios
- [ ] `SimpleAdmin.Tests/Helpers/TestWebApplicationFactory.cs` — `WebApplicationFactory<Program>` with test-specific seeder

---

## Sources

### Primary (HIGH confidence)

- [OpenIddict NuGet page — 7.3.0, .NET 10 confirmed](https://www.nuget.org/packages/OpenIddict) — version, framework targets, publish date
- [Creating your own server instance | OpenIddict docs](https://documentation.openiddict.com/guides/getting-started/creating-your-own-server-instance) — server configuration pattern, seeder IHostedService
- [Implementing token validation | OpenIddict docs](https://documentation.openiddict.com/guides/getting-started/implementing-token-validation-in-your-apis) — AddValidation, UseLocalServer, UseAspNetCore, default scheme
- [PKCE configuration | OpenIddict docs](https://documentation.openiddict.com/configuration/proof-key-for-code-exchange) — RequireProofKeyForCodeExchange, global vs per-client
- [Application permissions | OpenIddict docs](https://documentation.openiddict.com/configuration/application-permissions.html) — required Permissions constants for Authorization Code + PKCE
- [EF Core InMemoryDatabase API docs | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.inmemorydbcontextoptionsextensions.useinmemorydatabase?view=efcore-10.0) — InMemoryDatabaseRoot overload, shared root pattern
- [ASP.NET Core integration | OpenIddict docs](https://documentation.openiddict.com/integrations/aspnet-core) — passthrough mode, EnableAuthorizationEndpointPassthrough, middleware order

### Secondary (MEDIUM confidence)

- [Authorization Code Flow Part IV | dev.to (robinvanderknaap)](https://dev.to/robinvanderknaap/setting-up-an-authorization-server-with-openiddict-part-iv-authorization-code-flow-3eh8) — Authorize controller pattern with cookie challenge redirect, SetScopes, SignIn
- [EF Core integration | OpenIddict docs](https://documentation.openiddict.com/integrations/entity-framework-core) — UseOpenIddict() on DbContext options, entity sets
- [damienbod/AspNetCoreOpenIddict GitHub](https://github.com/damienbod/AspNetCoreOpeniddict) — Updated February 2026 for .NET 10; confirms current compatibility

### Tertiary (LOW confidence)

- Various community blog posts (Medium, andreyka26.com) — corroborate patterns but not independently verified against 7.3.0 API surface

---

## Metadata

**Confidence breakdown:**

- Standard stack: HIGH — NuGet 7.3.0 published March 9, 2026 with .NET 10 target confirmed directly from nuget.org
- Architecture: HIGH — Patterns drawn from official OpenIddict documentation and verified method signatures from EF Core 10 API reference
- Pitfalls: HIGH for items 1–5 (verified via official docs and GitHub issues); MEDIUM for pitfall 6 (confirmed by ASP.NET Core template knowledge)

**Research date:** 2026-03-11
**Valid until:** 2026-04-11 (OpenIddict minor versions release frequently; verify 7.x minor updates if >30 days pass)
