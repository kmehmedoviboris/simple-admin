using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleAdmin.Tests.Helpers;

namespace SimpleAdmin.Tests;

[Trait("Category", "Smoke")]
public class ProtectedEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProtectedEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
