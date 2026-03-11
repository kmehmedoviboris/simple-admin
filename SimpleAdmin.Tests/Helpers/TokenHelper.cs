using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace SimpleAdmin.Tests.Helpers;

/// <summary>
/// Shared utility for acquiring Bearer tokens via PKCE authorization code flow in integration tests.
/// </summary>
public static class TokenHelper
{
    private const string ClientId = "simple-admin-spa";
    private const string RedirectUri = "http://localhost:5173/callback";

    /// <summary>
    /// Acquires an access token using the PKCE authorization code flow.
    /// </summary>
    /// <param name="client">
    /// An HttpClient configured with AllowAutoRedirect = false and HandleCookies = true,
    /// created from TestWebApplicationFactory.
    /// </param>
    /// <param name="email">The user email to authenticate with. Defaults to admin@simpleadmin.local.</param>
    /// <param name="password">The user password. Defaults to Admin1234!</param>
    /// <returns>The access token string.</returns>
    /// <exception cref="InvalidOperationException">Thrown if any step of the flow fails.</exception>
    public static async Task<string> GetAccessTokenAsync(
        HttpClient client,
        string email = "admin@simpleadmin.local",
        string password = "Admin1234!")
    {
        // Step 1: Generate PKCE code_verifier and code_challenge
        var (codeVerifier, codeChallenge) = GeneratePkce();

        // Step 2: Build the authorize URL
        var authorizeUrl = $"/connect/authorize?response_type=code&client_id={ClientId}" +
                           $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                           $"&scope=openid%20email%20profile" +
                           $"&code_challenge={codeChallenge}" +
                           $"&code_challenge_method=S256" +
                           $"&state=test_state";

        // Step 3: GET /connect/authorize — expect 302 to /Account/Login
        var authResponse = await client.GetAsync(authorizeUrl);
        if (authResponse.StatusCode != HttpStatusCode.Redirect)
            throw new InvalidOperationException(
                $"Expected redirect to /Account/Login but got {(int)authResponse.StatusCode} {authResponse.StatusCode}");

        var loginUrl = authResponse.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("No Location header in authorize redirect");

        if (!loginUrl.Contains("/Account/Login"))
            throw new InvalidOperationException($"Expected redirect to /Account/Login but got: {loginUrl}");

        // Step 4: GET the login page and extract __RequestVerificationToken
        var loginPageResponse = await client.GetAsync(loginUrl);
        if (!loginPageResponse.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Failed to load login page: {(int)loginPageResponse.StatusCode} {loginPageResponse.StatusCode}");

        var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();

        var tokenMatch = Regex.Match(
            loginPageHtml,
            @"name=""__RequestVerificationToken""[^>]+value=""([^""]+)""");

        // Step 5: Extract ReturnUrl from the login URL query string
        var loginUri = new Uri(new Uri("http://localhost"), loginUrl);
        var queryParams = QueryHelpers.ParseQuery(loginUri.Query);
        var returnUrlParam = queryParams.TryGetValue("ReturnUrl", out var rv) ? rv.ToString() : "";

        // Step 6: POST the login form
        var formData = new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ReturnUrl"] = returnUrlParam
        };

        if (tokenMatch.Success)
        {
            formData["__RequestVerificationToken"] = tokenMatch.Groups[1].Value;
        }

        var loginPostResponse = await client.PostAsync(loginUrl, new FormUrlEncodedContent(formData));

        if (loginPostResponse.StatusCode != HttpStatusCode.Redirect)
            throw new InvalidOperationException(
                $"Expected redirect after login POST but got {(int)loginPostResponse.StatusCode} {loginPostResponse.StatusCode}");

        // Step 7: Follow redirect chain until reaching http://localhost:5173/callback?code=...
        var redirectUrl = loginPostResponse.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("No Location header after login POST");

        var redirectResponse = await client.GetAsync(redirectUrl);

        while (redirectResponse.StatusCode == HttpStatusCode.Redirect)
        {
            var nextUrl = redirectResponse.Headers.Location?.ToString()
                ?? throw new InvalidOperationException("No Location header in redirect chain");

            // Step 8: Extract the authorization code from the callback URL
            if (nextUrl.StartsWith("http://localhost:5173/"))
            {
                var callbackUri = new Uri(nextUrl);
                var callbackParams = QueryHelpers.ParseQuery(callbackUri.Query);

                var code = callbackParams.TryGetValue("code", out var c) ? c.ToString() : null;
                if (string.IsNullOrEmpty(code))
                    throw new InvalidOperationException(
                        $"No authorization code in callback URL: {nextUrl}");

                // Step 9: POST /connect/token with code + code_verifier
                var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = RedirectUri,
                    ["client_id"] = ClientId,
                    ["code_verifier"] = codeVerifier
                });

                var tokenResponse = await client.PostAsync("/connect/token", tokenRequest);
                if (!tokenResponse.IsSuccessStatusCode)
                    throw new InvalidOperationException(
                        $"Token endpoint returned {(int)tokenResponse.StatusCode} {tokenResponse.StatusCode}");

                // Step 10: Parse the JSON response and return the access_token
                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                var tokenDoc = JsonDocument.Parse(tokenJson);

                if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement))
                    throw new InvalidOperationException(
                        $"No access_token in token response: {tokenJson}");

                var accessToken = accessTokenElement.GetString()
                    ?? throw new InvalidOperationException("access_token is null in token response");

                return accessToken;
            }

            redirectResponse = await client.GetAsync(nextUrl);
        }

        throw new InvalidOperationException(
            "Did not receive redirect to callback URI with authorization code after following all redirects");
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
}
