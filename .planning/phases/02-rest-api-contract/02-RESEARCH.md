# Phase 2: REST API Contract - Research

**Researched:** 2026-03-11
**Domain:** ASP.NET Core 10 Web API / Identity UserManager / OpenAPI + Scalar
**Confidence:** HIGH

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| USER-01 | User can view a list of all users in a PrimeVue DataTable | GET /api/users endpoint returning `UserListDto[]`; UserManager.Users queryable; requires seeded users to exist at startup |
| USER-02 | User can create a new user by providing email and password | POST /api/users with `CreateUserDto`; UserManager.CreateAsync; IdentityResult error mapping to 400 |
| USER-03 | User can edit an existing user's email and optionally change their password | PUT /api/users/{id} with `UpdateUserDto`; UserManager.SetEmailAsync + SetUserNameAsync; RemovePasswordAsync + AddPasswordAsync for optional password change |
| USER-04 | User can delete a user after confirming via a confirmation dialog | DELETE /api/users/{id}; UserManager.FindByIdAsync then DeleteAsync; 404 on not found |
</phase_requirements>

---

## Summary

Phase 2 builds on the auth infrastructure from Phase 1 and adds four things: (1) DTO classes for the user management API surface, (2) a `UsersController` wiring those DTOs to `UserManager<ApplicationUser>`, (3) additional seeded users for smoke testing, and (4) an OpenAPI document served at `/openapi/v1.json` with a Scalar dev UI.

The project already uses `[ApiController]` conventions, Bearer validation, and `UserManager` — all registered and working in Phase 1. Phase 2 only needs to add controller actions, DTOs, and OpenAPI configuration; no changes to auth wiring, database, or middleware ordering are required.

The built-in `Microsoft.AspNetCore.OpenApi` package (shipping with .NET 9+, version 10.0.4 already in lockstep with the runtime) generates OpenAPI 3.1 documents at runtime served at `/openapi/v1.json`. `Scalar.AspNetCore` (latest 2.13.5) adds a visual dev UI at `/scalar`. Both integrate with a single `AddOpenApi()` + `MapOpenApi()` call.

**Primary recommendation:** Add `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore` packages; add a `UsersController` with four actions using `UserManager<ApplicationUser>` directly; extend the existing `OpenIddictWorker.StartAsync` to seed 2-3 additional test users alongside the existing admin user.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.AspNetCore.OpenApi | 10.0.4 | Runtime OpenAPI 3.1 document generation at `/openapi/v1.json` | First-party; ships with .NET 10; no Swashbuckle dependency; Context7 verified |
| Scalar.AspNetCore | 2.13.5 | Interactive OpenAPI dev UI at `/scalar` | Official Scalar integration; works directly with `MapOpenApi()`; NET 8+ compatible |
| Microsoft.AspNetCore.Identity (built-in) | 10.0.x | UserManager CRUD operations | Already registered in Phase 1 via AddIdentity; no new package needed |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.AspNetCore.Mvc (built-in) | 10.0.x | `[ApiController]`, `[ProducesResponseType]`, `ActionResult<T>` | Standard controller infrastructure already in project |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Microsoft.AspNetCore.OpenApi | Swashbuckle | Swashbuckle not updated for .NET 9+ WebAPI conventions; Microsoft.AspNetCore.OpenApi is the new first-party standard |
| Scalar.AspNetCore | Swagger UI (NSwag) | Scalar is the recommended UI in .NET 10 new-project templates; Swagger UI still works but requires separate NuGet |
| Direct UserManager CRUD | Repository pattern / service layer | Unnecessary abstraction for a POC with 4 endpoints; UserManager is already the service layer |

**Installation:**
```bash
dotnet add package Microsoft.AspNetCore.OpenApi --version 10.0.4
dotnet add package Scalar.AspNetCore --version 2.13.5
```

---

## Architecture Patterns

### Recommended Project Structure

```
SimpleAdmin.Api/
├── Controllers/
│   ├── ApiController.cs           # Existing: GET /api/me (Phase 1)
│   ├── AuthorizationController.cs # Existing: /connect/authorize, /connect/token (Phase 1)
│   └── UsersController.cs         # NEW: GET/POST/PUT/DELETE /api/users
├── Data/
│   └── ApplicationDbContext.cs    # Existing (unchanged)
├── Dtos/                          # NEW folder
│   ├── UserListDto.cs             # id, email, emailConfirmed
│   ├── CreateUserDto.cs           # email, password
│   └── UpdateUserDto.cs           # email (optional), newPassword (optional)
├── Models/
│   └── ApplicationUser.cs         # Existing (unchanged)
├── Pages/                         # Existing (unchanged)
├── Workers/
│   └── OpenIddictWorker.cs        # EXTENDED: add 2-3 test users in StartAsync
└── Program.cs                     # EXTENDED: AddOpenApi(), MapOpenApi(), MapScalarApiReference()
```

### Pattern 1: OpenAPI + Scalar Registration

**What:** Add `builder.Services.AddOpenApi()` to service registration, then `app.MapOpenApi()` and `app.MapScalarApiReference()` in the middleware pipeline (development-only is sufficient for this POC).

**When to use:** Required for success criterion 5 — navigating to `/openapi/v1.json` returns the spec document.

**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0
// In Program.cs — service registration
builder.Services.AddOpenApi();

// In Program.cs — middleware pipeline (after app.Build())
// MapOpenApi() and MapScalarApiReference() do NOT require IsDevelopment() guard for POC,
// but it is best practice to guard them.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                  // serves /openapi/v1.json
    app.MapScalarApiReference();       // serves /scalar (default)
}
```

After the app is running, `/openapi/v1.json` and `/scalar` are accessible. No auth is applied to these endpoints by default — suitable for this POC.

### Pattern 2: UsersController with UserManager

**What:** A standard `[ApiController]` that injects `UserManager<ApplicationUser>` and exposes four REST endpoints. All endpoints require Bearer auth via `[Authorize]`.

**When to use:** The four USER-0x requirements all map directly to this controller.

**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
[ApiController]
[Route("api/users")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    // GET /api/users
    [HttpGet]
    [ProducesResponseType<IEnumerable<UserListDto>>(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var users = _userManager.Users
            .Select(u => new UserListDto(u.Id!, u.Email!, u.EmailConfirmed))
            .ToList();
        return Ok(users);
    }

    // POST /api/users
    [HttpPost]
    [ProducesResponseType<UserListDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetAll), new UserListDto(user.Id!, user.Email!, user.EmailConfirmed));
    }

    // PUT /api/users/{id}
    [HttpPut("{id}")]
    [ProducesResponseType<UserListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        if (dto.Email is not null)
        {
            var setEmail = await _userManager.SetEmailAsync(user, dto.Email);
            if (!setEmail.Succeeded) return BadRequest(setEmail.Errors);
            await _userManager.SetUserNameAsync(user, dto.Email); // keep UserName in sync
        }

        if (dto.NewPassword is not null)
        {
            var remove = await _userManager.RemovePasswordAsync(user);
            if (!remove.Succeeded) return BadRequest(remove.Errors);
            var add = await _userManager.AddPasswordAsync(user, dto.NewPassword);
            if (!add.Succeeded) return BadRequest(add.Errors);
        }

        return Ok(new UserListDto(user.Id!, user.Email!, user.EmailConfirmed));
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        await _userManager.DeleteAsync(user);
        return NoContent();
    }
}
```

### Pattern 3: DTOs (simple records)

**What:** Lightweight C# records that define the API contract. No AutoMapper needed.

**When to use:** DTOs decouple the API surface from `IdentityUser` internals — avoids accidentally serializing password hashes or security stamps.

**Example:**
```csharp
// Dtos/UserListDto.cs
public record UserListDto(string Id, string Email, bool EmailConfirmed);

// Dtos/CreateUserDto.cs
public record CreateUserDto(string Email, string Password);

// Dtos/UpdateUserDto.cs
// All properties optional — PUT can update email, password, or both
public record UpdateUserDto(string? Email, string? NewPassword);
```

### Pattern 4: Admin Password Change (No Old Password Required)

**What:** `UserManager.RemovePasswordAsync` + `UserManager.AddPasswordAsync` — the two-step admin pattern for setting a new password without requiring the current password.

**When to use:** PUT /api/users/{id} when `UpdateUserDto.NewPassword` is provided. This is an administrative endpoint, not a self-service change; the old password is irrelevant.

**Source:** [UserManager.RemovePasswordAsync | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.removepasswordasync?view=aspnetcore-10.0)

```csharp
// Admin password reset — no old password required
var remove = await _userManager.RemovePasswordAsync(user);
if (!remove.Succeeded) return BadRequest(remove.Errors);

var add = await _userManager.AddPasswordAsync(user, dto.NewPassword);
if (!add.Succeeded) return BadRequest(add.Errors);
```

Do NOT use `ChangePasswordAsync` — it requires the current password and is for self-service flows.

### Pattern 5: Seed Additional Test Users in OpenIddictWorker

**What:** Extend the existing `OpenIddictWorker.StartAsync` to seed 2-3 additional test users alongside the admin. The admin user already exists; add test users only if they don't exist.

**When to use:** Required for success criterion 1 — GET /api/users returns a list. With only one seeded user the list test is trivially uninteresting.

**Example:**
```csharp
// In OpenIddictWorker.StartAsync, after the admin user seed block:
var testUsers = new[]
{
    ("alice@simpleadmin.local", "Alice1234!"),
    ("bob@simpleadmin.local", "Bob1234!")
};

foreach (var (email, password) in testUsers)
{
    if (await userManager.FindByEmailAsync(email) is null)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(user, password);
    }
}
```

### Anti-Patterns to Avoid

- **Using `ChangePasswordAsync` for admin password resets:** `ChangePasswordAsync` validates the current password. Use `RemovePasswordAsync` + `AddPasswordAsync` for admin-initiated changes.
- **Serializing `ApplicationUser` directly from the controller:** `ApplicationUser` inherits from `IdentityUser` which includes `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp` — sensitive fields that must not appear in API responses. Always project to a DTO.
- **Calling `_userManager.Users.ToListAsync()`:** `UserManager.Users` returns `IQueryable<TUser>`, but `ToListAsync()` requires `Microsoft.EntityFrameworkCore` namespace. Use `.ToList()` (synchronous) or `.AsAsyncEnumerable()` to avoid confusion. For this POC with in-memory storage, `.ToList()` is fine.
- **Forgetting `SetUserNameAsync` when changing email:** `IdentityUser.UserName` and `Email` are separate fields. `SetEmailAsync` only updates `Email`; the `UserName` used for login must be updated separately with `SetUserNameAsync`. Without this, `FindByEmailAsync` succeeds but login with the new email fails because UserName still holds the old value.
- **`MapOpenApi()` before `app.UseAuthentication()`:** The OpenAPI endpoint is a regular route endpoint. If placed before auth middleware it has no effect on document generation, but any `RequireAuthorization()` call on it will silently fail. Keep `MapOpenApi()` after `UseAuthentication()` / `UseAuthorization()`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Password hashing | Custom bcrypt/argon2 | `UserManager.CreateAsync` + `AddPasswordAsync` | PBKDF2 with salting, hash upgrading, configurable iterations — all handled |
| Password validation | Custom regex | `UserManager` password validators | Configurable policies (length, complexity) already wired from Phase 1 `AddIdentity` call |
| OpenAPI document | Custom JSON builder | `Microsoft.AspNetCore.OpenApi` | Response type inference, schema generation, operation IDs — massive surface area |
| IdentityResult → HTTP error | Custom error mapper | Return `BadRequest(result.Errors)` | `IdentityError` is already JSON-serializable with `Code` + `Description`; no mapping needed |
| User lookup | Custom SQL | `UserManager.FindByIdAsync` / `FindByEmailAsync` | Handles concurrency stamps, normalized email lookup, case-insensitive matching |

**Key insight:** `UserManager<T>` is already a complete user service. Wrapping it in a repository or service layer adds no value for a 4-endpoint POC and costs test setup complexity.

---

## Common Pitfalls

### Pitfall 1: Email and UserName Out of Sync

**What goes wrong:** After PUT /api/users/{id} updates the email, subsequent login attempts fail even with the new email and correct password.
**Why it happens:** ASP.NET Core Identity stores `UserName` and `Email` separately. The login flow uses `UserName` to find the user (`FindByNameAsync` internally), not `Email`. `SetEmailAsync` updates only `Email` and `NormalizedEmail`; `UserName` is left unchanged.
**How to avoid:** Always call `SetUserNameAsync(user, dto.Email)` immediately after `SetEmailAsync` when the email changes.
**Warning signs:** `FindByEmailAsync` succeeds but `SignInManager.PasswordSignInAsync` fails for the updated email.

### Pitfall 2: `_userManager.Users` Query Executes Without `ToList`

**What goes wrong:** The GET /api/users action returns an empty list or throws `InvalidOperationException`.
**Why it happens:** `UserManager.Users` is an `IQueryable<TUser>`. LINQ projection with `.Select()` produces a new `IQueryable` that is not yet evaluated. With in-memory EF Core, the query materializes only when `ToList()` / `ToArray()` / iteration is called. Returning the `IQueryable` directly from the controller causes `System.Text.Json` to attempt serialization of the query object, not its results.
**How to avoid:** Always call `.ToList()` before returning. Example: `_userManager.Users.Select(...).ToList()`.
**Warning signs:** GET /api/users returns `[]` when seeded users definitely exist, or throws during serialization.

### Pitfall 3: OpenAPI Endpoint Not Appearing Because `MapOpenApi()` Is Placed Wrong

**What goes wrong:** `/openapi/v1.json` returns 404.
**Why it happens:** `MapOpenApi()` registers a route endpoint. If it is called before `app.Build()` executes or is placed outside the request pipeline (e.g., in service registration), it is a no-op.
**How to avoid:** Place `app.MapOpenApi()` after `var app = builder.Build()` and after `app.UseRouting()`. For this project, it goes right after `app.UseAuthorization()` and before `app.MapControllers()`.
**Warning signs:** `/openapi/v1.json` returns 404; no route is shown in startup logs for `openapi/v1.json`.

### Pitfall 4: IdentityResult Errors Not Propagated Correctly

**What goes wrong:** POST /api/users returns 400 but with an empty or malformed body; the Vue client cannot display validation errors.
**Why it happens:** `BadRequest(result.Errors)` returns the `IEnumerable<IdentityError>` directly. This serializes correctly as a JSON array of `{ "code": "...", "description": "..." }` objects. However, some developers accidentally return `BadRequest(result.Errors.First().Description)` — a bare string — which makes client-side error handling fragile.
**How to avoid:** Return `BadRequest(result.Errors)` — the full collection. The Vue client in Phase 4 will iterate errors.
**Warning signs:** Error responses are a plain string instead of a JSON array.

### Pitfall 5: `ProducesResponseType` Missing — Scalar/OpenAPI Shows No Response Schema

**What goes wrong:** The OpenAPI document at `/openapi/v1.json` shows `UsersController` endpoints with no response body schemas; the Scalar UI cannot render example responses.
**Why it happens:** `[ApiController]` + `[ProducesResponseType<T>]` is how `Microsoft.AspNetCore.OpenApi` infers response schemas for MVC controllers. Without `[ProducesResponseType<UserListDto>(...)]`, the endpoint is registered with no typed response.
**How to avoid:** Every action that returns a DTO must have `[ProducesResponseType<TDto>(StatusCodes.Status200OK)]` (or 201 for POST). The generic form `[ProducesResponseType<T>]` requires .NET 7+ and is supported in .NET 10.
**Warning signs:** `/openapi/v1.json` shows response schemas as `{}` or entirely missing for GET/POST endpoints.

### Pitfall 6: Scalar UI URL vs. OpenAPI URL Confusion

**What goes wrong:** The success criterion says `/openapi/v1.json` must work. The Scalar dev UI lives at `/scalar` (default). These are different URLs. Navigating to `/scalar` shows the UI; `/openapi/v1.json` returns raw JSON.
**Why it happens:** Developers sometimes point the Phase 3 `hey-api/openapi-ts` client generator at `/scalar` instead of `/openapi/v1.json`.
**How to avoid:** Document clearly: spec JSON at `/openapi/v1.json`, dev UI at `/scalar`. Phase 3 client generation uses `/openapi/v1.json`.
**Warning signs:** `openapi-ts` fails to parse the Scalar HTML page as an OpenAPI spec.

---

## Code Examples

### Program.cs Changes for Phase 2

```csharp
// Source: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0
// Add after existing builder.Services registrations:
builder.Services.AddOpenApi();

// Add after var app = builder.Build():
// (existing middleware chain)
// app.UseRouting();
// app.UseCors();
// app.UseAuthentication();
// app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();             // → /openapi/v1.json
    app.MapScalarApiReference();  // → /scalar
}

app.MapControllers();
app.MapRazorPages();
```

### GET /api/users — UserManager.Users Projection

```csharp
// Source: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0
[HttpGet]
[ProducesResponseType<IEnumerable<UserListDto>>(StatusCodes.Status200OK)]
public IActionResult GetAll()
{
    var users = _userManager.Users
        .Select(u => new UserListDto(u.Id!, u.Email!, u.EmailConfirmed))
        .ToList();  // materialize — never return IQueryable from a controller
    return Ok(users);
}
```

### POST /api/users — CreateAsync with IdentityResult Error Handling

```csharp
// Source: https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
[HttpPost]
[ProducesResponseType<UserListDto>(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
{
    var user = new ApplicationUser
    {
        UserName = dto.Email,
        Email = dto.Email,
        EmailConfirmed = true
    };
    var result = await _userManager.CreateAsync(user, dto.Password);
    if (!result.Succeeded)
        return BadRequest(result.Errors);  // IEnumerable<IdentityError> — JSON-serializable

    return CreatedAtAction(nameof(GetAll), new UserListDto(user.Id!, user.Email!, user.EmailConfirmed));
}
```

### PUT /api/users/{id} — Email + Optional Password Update

```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.removepasswordasync?view=aspnetcore-10.0
[HttpPut("{id}")]
[ProducesResponseType<UserListDto>(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
{
    var user = await _userManager.FindByIdAsync(id);
    if (user is null) return NotFound();

    if (dto.Email is not null && dto.Email != user.Email)
    {
        var emailResult = await _userManager.SetEmailAsync(user, dto.Email);
        if (!emailResult.Succeeded) return BadRequest(emailResult.Errors);
        // CRITICAL: keep UserName in sync with Email for login to work
        await _userManager.SetUserNameAsync(user, dto.Email);
    }

    if (dto.NewPassword is not null)
    {
        var remove = await _userManager.RemovePasswordAsync(user);
        if (!remove.Succeeded) return BadRequest(remove.Errors);
        var add = await _userManager.AddPasswordAsync(user, dto.NewPassword);
        if (!add.Succeeded) return BadRequest(add.Errors);
    }

    return Ok(new UserListDto(user.Id!, user.Email!, user.EmailConfirmed));
}
```

### DELETE /api/users/{id}

```csharp
[HttpDelete("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Delete(string id)
{
    var user = await _userManager.FindByIdAsync(id);
    if (user is null) return NotFound();
    await _userManager.DeleteAsync(user);
    return NoContent();
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Swashbuckle for OpenAPI | `Microsoft.AspNetCore.OpenApi` (first-party) | .NET 9 (2024) | Swashbuckle is not updated for .NET 9+ WebAPI conventions; use the built-in package |
| Swagger UI | Scalar UI | .NET 9+ new-project templates (2024) | Scalar is the new default in `dotnet new webapi`; Swashbuckle/Swagger UI still works but deprecated path |
| Separate DTO library | Inline records | Always valid; records since C# 9 (2020) | `record` types are ideal for immutable DTOs — value equality, deconstruct, minimal syntax |
| `ChangePasswordAsync` for admin resets | `RemovePasswordAsync` + `AddPasswordAsync` | Always the correct pattern for admin use | `ChangePasswordAsync` requires current password — wrong API for admin-initiated changes |

**Deprecated/outdated:**
- `Swashbuckle.AspNetCore`: Not updated for `[ApiController]` + `IEndpointMetadataContext` conventions in .NET 9/10. Do not add to this project.
- `NSwag`: Still maintained but not needed; `Microsoft.AspNetCore.OpenApi` covers all required use cases.
- `ChangePasswordAsync` for admin use: Wrong API; silently fails if no current password set (returns IdentityError).

---

## Open Questions

1. **OpenAPI bearer security scheme documentation**
   - What we know: The OpenAPI document will be generated but JWT/Bearer security won't be annotated unless a security scheme transformer is added.
   - What's unclear: Whether the Phase 3 `hey-api/openapi-ts` generator requires a security scheme in the spec to generate a properly authenticated client.
   - Recommendation: Add a `UseSecurityScheme` transformer to `AddOpenApi()` for the Bearer scheme; it is low effort and prevents re-visiting in Phase 3. Mark as optional for Phase 2 — the success criteria do not mention security scheme annotation.

2. **Scalar UI URL conflicts with OpenIddict**
   - What we know: `MapScalarApiReference()` registers `/scalar` as a route. OpenIddict reserves `/connect/*` routes.
   - What's unclear: Whether any Scalar route conflicts with existing routes.
   - Recommendation: No conflict expected — Scalar uses `/scalar` prefix by default, not `/connect/*` or `/api/*`.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.AspNetCore.Mvc.Testing 10.0.4 (already in `SimpleAdmin.Tests`) |
| Config file | `SimpleAdmin.Tests/SimpleAdmin.Tests.csproj` (exists) |
| Quick run command | `dotnet test --filter "Category=Smoke"` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| USER-01 | GET /api/users with Bearer returns 200 + non-empty list | Integration (smoke) | `dotnet test --filter "FullyQualifiedName~UsersControllerTests"` | Wave 0 |
| USER-02 | POST /api/users with valid body creates user, returns 201 | Integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests"` | Wave 0 |
| USER-02 | POST /api/users with duplicate email returns 400 | Integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests"` | Wave 0 |
| USER-03 | PUT /api/users/{id} updates email, returns 200 | Integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests"` | Wave 0 |
| USER-03 | PUT /api/users/{id} updates password — subsequent requests authenticated | Integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests"` | Wave 0 |
| USER-04 | DELETE /api/users/{id} removes user, subsequent GET confirms absence | Integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests"` | Wave 0 |
| USER-04 | DELETE /api/users/{nonexistent} returns 404 | Integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests"` | Wave 0 |
| (all) | All /api/users endpoints return 401 without Bearer token | Integration (smoke) | `dotnet test --filter "Category=Smoke"` | Wave 0 |

**Token acquisition for tests:** The existing `AuthFlowSmokeTests.FullAuthCodeFlow_ReturnsAccessToken` already demonstrates acquiring a token through the full PKCE flow. Phase 2 tests should extract this into a helper method on `TestWebApplicationFactory` or a shared `TokenHelper` to avoid duplicating the PKCE dance in every test.

### Sampling Rate

- **Per task commit:** `dotnet test --filter "Category=Smoke"` (401 check + GET returns 200)
- **Per wave merge:** `dotnet test` (full suite including USER-02 through USER-04 scenarios)
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `SimpleAdmin.Tests/UsersControllerTests.cs` — covers USER-01 through USER-04 integration scenarios
- [ ] `SimpleAdmin.Tests/Helpers/TokenHelper.cs` (or method on `TestWebApplicationFactory`) — shared PKCE token acquisition to avoid duplicating the auth flow in every test class

*(Existing infrastructure: `TestWebApplicationFactory.cs`, `CookieTrackingHandler.cs`, `AuthFlowSmokeTests.cs`, `ProtectedEndpointTests.cs` — all exist and need no changes for Phase 2)*

---

## Sources

### Primary (HIGH confidence)

- [Generate OpenAPI documents | Microsoft Learn (ASP.NET Core 10.0)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0) — `AddOpenApi()`, `MapOpenApi()`, default URL `/openapi/v1.json`, updated 2026-02-11
- [NuGet: Microsoft.AspNetCore.OpenApi 10.0.4](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi) — version confirmation
- [NuGet: Scalar.AspNetCore 2.13.5](https://www.nuget.org/packages/Scalar.AspNetCore) — latest version, .NET 8+ compatibility
- [UserManager.RemovePasswordAsync | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.removepasswordasync?view=aspnetcore-10.0) — admin password reset pattern verified for .NET 10
- [Controller action return types | Microsoft Learn (ASP.NET Core 10.0)](https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0) — `[ProducesResponseType<T>]`, `ActionResult<T>` patterns

### Secondary (MEDIUM confidence)

- [Scalar.AspNetCore integration guide](https://guides.scalar.com/scalar/scalar-api-references/integrations/net-aspnet-core) — `MapScalarApiReference()` call, `/scalar` default URL; confirmed by multiple NuGet gallery descriptions
- [ASP.NET Core Identity CRUD patterns | yogihosting.com](https://www.yogihosting.com/aspnet-core-identity-create-read-update-delete-users/) — UserManager CRUD patterns (corroborated by official docs)

### Tertiary (LOW confidence)

- Various Medium posts on Scalar + ASP.NET Core — not independently verified against 2.13.5 API surface; patterns consistent with official docs

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — NuGet packages verified (10.0.4, 2.13.5); official MS docs confirmed `AddOpenApi`/`MapOpenApi` signatures
- Architecture: HIGH — UserManager CRUD patterns are stable across Identity versions; verified against official docs and .NET 10 API reference
- Pitfalls: HIGH for email/username sync, IQueryable materialization, `ChangePasswordAsync` vs admin pattern (all verified by official docs); MEDIUM for OpenAPI endpoint ordering (convention-based, not explicitly documented)

**Research date:** 2026-03-11
**Valid until:** 2026-04-11 (Scalar releases frequently; check for minor version updates if >30 days pass)
