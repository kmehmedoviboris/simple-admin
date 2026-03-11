using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SimpleAdmin.Api.Data;
using SimpleAdmin.Api.Models;

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

builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
