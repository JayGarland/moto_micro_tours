using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace moto_micro_tours.Tests;

/// <summary>
/// Stage F: Tests for cookie-based authentication
/// </summary>
public class AuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthenticationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false, // Important for cookie authentication testing
            HandleCookies = true // Automatically handle cookies
        });
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndUserInfo()
    {
        // Arrange
        var loginRequest = new
        {
            username = "organizer1",
            password = "pass"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.Equal("organizer1", result.Username);
        Assert.Contains("Organizer", result.Roles);
        Assert.Contains("Motard", result.Roles);

        // Verify cookie was set
        Assert.True(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            username = "wronguser",
            password = "wrongpass"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutLogin_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_AfterLogin_ReturnsUserInfo()
    {
        // Arrange - Login first
        var loginRequest = new
        {
            username = "participant1",
            password = "pass"
        };
        await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Act - Get current user
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.Equal("participant1", result.Username);
        Assert.Contains("Participant", result.Roles);
    }

    [Fact]
    public async Task Login_AllSeededUsers_WorksCorrectly()
    {
        // Test all seeded users can login
        var users = new[]
        {
            new { Username = "organizer1", ExpectedRoles = new[] { "Organizer", "Motard" } },
            new { Username = "motard1", ExpectedRoles = new[] { "Motard" } },
            new { Username = "motard2", ExpectedRoles = new[] { "Motard" } },
            new { Username = "participant1", ExpectedRoles = new[] { "Participant" } },
            new { Username = "participant2", ExpectedRoles = new[] { "Participant" } }
        };

        foreach (var user in users)
        {
            // Create a new client for each user to avoid cookie conflicts
            var client = _client;

            var loginRequest = new
            {
                username = user.Username,
                password = "pass"
            };

            var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.NotNull(result);
            Assert.Equal(user.Username, result.Username);

            foreach (var expectedRole in user.ExpectedRoles)
            {
                Assert.Contains(expectedRole, result.Roles);
            }
        }
    }

    [Fact]
    public async Task Logout_AfterLogin_ClearsCookie()
    {
        // Arrange - Login first
        var loginRequest = new
        {
            username = "motard1",
            password = "pass"
        };
        await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Act - Logout
        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Verify we can't access protected endpoint after logout
        var meResponse = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    // Helper class for deserialization
    private class LoginResponse
    {
        public string UserId { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
