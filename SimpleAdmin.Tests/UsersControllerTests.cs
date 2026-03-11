using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleAdmin.Api.Dtos;
using SimpleAdmin.Tests.Helpers;

namespace SimpleAdmin.Tests;

public class UsersControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UsersControllerTests(TestWebApplicationFactory factory) => _factory = factory;

    /// <summary>
    /// Creates an authenticated HttpClient with a Bearer token for admin@simpleadmin.local.
    /// </summary>
    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var token = await TokenHelper.GetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ===== USER-01: GET /api/users =====

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetAll_WithBearerToken_ReturnsSeededUsers()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserListDto>>(body, JsonOptions);

        Assert.NotNull(users);
        Assert.True(users.Count >= 3, $"Expected at least 3 seeded users but got {users.Count}");
        Assert.Contains(users, u => u.Email == "admin@simpleadmin.local");
    }

    // ===== USER-02: POST /api/users =====

    [Fact]
    public async Task Create_WithValidBody_Returns201()
    {
        var client = await CreateAuthenticatedClientAsync();
        var uniqueEmail = $"create-valid-{Guid.NewGuid():N}@test.local";

        var body = JsonSerializer.Serialize(new { email = uniqueEmail, password = "Test1234!" });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/users", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<UserListDto>(responseBody, JsonOptions);

        Assert.NotNull(created);
        Assert.Equal(uniqueEmail, created.Email);

        // Confirm the new user appears in the GET list
        var listResponse = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listBody = await listResponse.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserListDto>>(listBody, JsonOptions);

        Assert.NotNull(users);
        Assert.Contains(users, u => u.Email == uniqueEmail);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();

        var body = JsonSerializer.Serialize(new { email = "admin@simpleadmin.local", password = "Test1234!" });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/users", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ===== USER-03: PUT /api/users/{id} =====

    [Fact]
    public async Task Update_EmailChange_Returns200()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create a user to update
        var originalEmail = $"update-email-{Guid.NewGuid():N}@test.local";
        var createBody = JsonSerializer.Serialize(new { email = originalEmail, password = "Test1234!" });
        var createResponse = await client.PostAsync("/api/users", new StringContent(createBody, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdUser = JsonSerializer.Deserialize<UserListDto>(createResponseBody, JsonOptions);
        Assert.NotNull(createdUser);

        // Update email
        var updatedEmail = $"updated-{Guid.NewGuid():N}@test.local";
        var updateBody = JsonSerializer.Serialize(new { email = updatedEmail });
        var updateResponse = await client.PutAsync(
            $"/api/users/{createdUser.Id}",
            new StringContent(updateBody, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updateResponseBody = await updateResponse.Content.ReadAsStringAsync();
        var updatedUser = JsonSerializer.Deserialize<UserListDto>(updateResponseBody, JsonOptions);

        Assert.NotNull(updatedUser);
        Assert.Equal(updatedEmail, updatedUser.Email);
    }

    [Fact]
    public async Task Update_PasswordChange_Returns200()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create a user to update
        var email = $"update-pass-{Guid.NewGuid():N}@test.local";
        var createBody = JsonSerializer.Serialize(new { email, password = "Test1234!" });
        var createResponse = await client.PostAsync("/api/users", new StringContent(createBody, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdUser = JsonSerializer.Deserialize<UserListDto>(createResponseBody, JsonOptions);
        Assert.NotNull(createdUser);

        // Update password
        var updateBody = JsonSerializer.Serialize(new { newPassword = "NewPass1234!" });
        var updateResponse = await client.PutAsync(
            $"/api/users/{createdUser.Id}",
            new StringContent(updateBody, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentUser_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var updateBody = JsonSerializer.Serialize(new { email = "x@x.com" });
        var updateResponse = await client.PutAsync(
            "/api/users/nonexistent-id-000",
            new StringContent(updateBody, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    }

    // ===== USER-04: DELETE /api/users/{id} =====

    [Fact]
    public async Task Delete_ExistingUser_Returns204()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create a user to delete
        var email = $"delete-me-{Guid.NewGuid():N}@test.local";
        var createBody = JsonSerializer.Serialize(new { email, password = "Test1234!" });
        var createResponse = await client.PostAsync("/api/users", new StringContent(createBody, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdUser = JsonSerializer.Deserialize<UserListDto>(createResponseBody, JsonOptions);
        Assert.NotNull(createdUser);

        // Delete the user
        var deleteResponse = await client.DeleteAsync($"/api/users/{createdUser.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Confirm the deleted user is absent from GET list
        var listResponse = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listBody = await listResponse.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserListDto>>(listBody, JsonOptions);

        Assert.NotNull(users);
        Assert.DoesNotContain(users, u => u.Email == email);
    }

    [Fact]
    public async Task Delete_NonExistentUser_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var deleteResponse = await client.DeleteAsync("/api/users/nonexistent-id-000");

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    // ===== Auth enforcement =====

    [Fact]
    [Trait("Category", "Smoke")]
    public async Task GetAll_WithoutBearerToken_Returns401()
    {
        // Plain client without any Authorization header
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===== OpenAPI spec =====

    [Fact]
    public async Task OpenApiSpec_Returns200WithPaths()
    {
        // OpenAPI endpoint is unauthenticated — no auth header needed
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/users", body);
    }
}
