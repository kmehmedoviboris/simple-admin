using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using SimpleAdmin.Tests.Helpers;

namespace SimpleAdmin.Tests;

[Trait("Category", "Smoke")]
public class AuthFlowSmokeTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthFlowSmokeTests(TestWebApplicationFactory factory) => _factory = factory;

    private static string BuildAuthorizeUrl(string codeChallenge)
    {
        return $"/connect/authorize?response_type=code&client_id=simple-admin-spa" +
               $"&redirect_uri={Uri.EscapeDataString("http://localhost:5173/callback")}" +
               $"&scope=openid%20email%20profile" +
               $"&code_challenge={codeChallenge}" +
               $"&code_challenge_method=S256" +
               $"&state=test_state";
    }

    private static (string codeVerifier, string codeChallenge) GeneratePkce()
    {
        var codeVerifier = "test_code_verifier_that_is_long_enough_for_pkce_validation_requirements";
        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        var codeChallenge = Convert.ToBase64String(challengeBytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (codeVerifier, codeChallenge);
    }

    [Fact]
    public async Task AuthorizeEndpoint_RedirectsToLogin_WhenUnauthenticated()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var (_, codeChallenge) = GeneratePkce();
        var authorizeUrl = BuildAuthorizeUrl(codeChallenge);

        var response = await client.GetAsync(authorizeUrl);

        // Should redirect to login page (302 to /Account/Login)
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task FullAuthCodeFlow_ReturnsAccessToken()
    {
        var factoryClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var (codeVerifier, codeChallenge) = GeneratePkce();
        var authorizeUrl = BuildAuthorizeUrl(codeChallenge);

        // Step 1: Hit authorize endpoint — expect redirect to /Account/Login
        var authResponse = await factoryClient.GetAsync(authorizeUrl);
        Assert.Equal(HttpStatusCode.Redirect, authResponse.StatusCode);
        var loginUrl = authResponse.Headers.Location!.ToString();
        Assert.Contains("/Account/Login", loginUrl);

        // Step 2: GET the login page (to get the antiforgery token)
        var loginPageResponse = await factoryClient.GetAsync(loginUrl);
        Assert.True(loginPageResponse.IsSuccessStatusCode);
        var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();

        // Extract the antiforgery token from the form
        // The rendered HTML is: name="__RequestVerificationToken" type="hidden" value="..."
        var tokenMatch = Regex.Match(
            loginPageHtml,
            @"name=""__RequestVerificationToken""[^>]+value=""([^""]+)""");

        // Extract ReturnUrl from the login page URL query string
        var loginUri = new Uri(new Uri("http://localhost"), loginUrl);
        var queryParams = QueryHelpers.ParseQuery(loginUri.Query);
        var returnUrlParam = queryParams.TryGetValue("ReturnUrl", out var rv) ? rv.ToString() : "";

        var formData = new Dictionary<string, string>
        {
            ["Email"] = "admin@simpleadmin.local",
            ["Password"] = "Admin1234!",
            ["ReturnUrl"] = returnUrlParam
        };

        if (tokenMatch.Success)
        {
            formData["__RequestVerificationToken"] = tokenMatch.Groups[1].Value;
        }

        // Step 3: POST login credentials
        var loginPostResponse = await factoryClient.PostAsync(
            loginUrl,
            new FormUrlEncodedContent(formData));

        // Should redirect back to authorize endpoint (which then redirects to callback with code)
        Assert.Equal(HttpStatusCode.Redirect, loginPostResponse.StatusCode);

        // Step 4: Follow the redirect chain until we get the callback with the code
        var redirectUrl = loginPostResponse.Headers.Location!.ToString();
        var redirectResponse = await factoryClient.GetAsync(redirectUrl);

        // Keep following redirects until we reach the external redirect_uri
        // (which will be a redirect to http://localhost:5173/callback?code=...&state=...)
        while (redirectResponse.StatusCode == HttpStatusCode.Redirect)
        {
            var nextUrl = redirectResponse.Headers.Location!.ToString();
            if (nextUrl.StartsWith("http://localhost:5173/"))
            {
                // We got the callback redirect with the authorization code
                var callbackUri = new Uri(nextUrl);
                var callbackParams = QueryHelpers.ParseQuery(callbackUri.Query);
                var code = callbackParams.TryGetValue("code", out var c) ? c.ToString() : null;

                Assert.NotNull(code);
                Assert.NotEmpty(code);

                // Step 5: Exchange code for token
                var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = "http://localhost:5173/callback",
                    ["client_id"] = "simple-admin-spa",
                    ["code_verifier"] = codeVerifier
                });

                var tokenResponse = await factoryClient.PostAsync("/connect/token", tokenRequest);
                Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                Assert.Contains("access_token", tokenJson);

                // Step 6: Use the access token to call /api/me
                var tokenDoc = System.Text.Json.JsonDocument.Parse(tokenJson);
                var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

                factoryClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var meResponse = await factoryClient.GetAsync("/api/me");
                Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

                var meJson = await meResponse.Content.ReadAsStringAsync();
                Assert.Contains("admin@simpleadmin.local", meJson);

                return; // Test passed
            }
            redirectResponse = await factoryClient.GetAsync(nextUrl);
        }

        // If we get here, we never reached the callback
        Assert.Fail("Did not receive redirect to callback URI with authorization code");
    }
}
