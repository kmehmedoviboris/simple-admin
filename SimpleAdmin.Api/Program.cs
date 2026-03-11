using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OpenIddict.Validation.AspNetCore;
using Scalar.AspNetCore;
using SimpleAdmin.Api.Data;
using SimpleAdmin.Api.Models;
using SimpleAdmin.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// Shared in-memory database root so all DbContext instances share the same data
var inMemoryRoot = new InMemoryDatabaseRoot();
builder.Services.AddSingleton(inMemoryRoot);

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseInMemoryDatabase("SimpleAdmin", sp.GetRequiredService<InMemoryDatabaseRoot>());
    options.UseOpenIddict();
});

// Use AddIdentity (not AddDefaultIdentity) to avoid default UI conflicts with OpenIddict
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

// DefaultScheme MUST be OpenIddictValidationAspNetCoreDefaults (not JwtBearer, not Cookie)
// Cookie is only the DefaultChallengeScheme for login redirects
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
builder.Services.AddRazorPages();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHostedService<OpenIddictWorker>();

var app = builder.Build();

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();             // serves /openapi/v1.json
    app.MapScalarApiReference();  // serves /scalar
}

app.MapControllers();
app.MapRazorPages();

app.Run();

public partial class Program { }
