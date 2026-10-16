using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Middleware;
using Xunit;

namespace moto_micro_tours.Tests;

/// <summary>
/// Idempotency-Key cache behavior (P0-adjacent authorization boundary fix):
/// a cached response must never be replayed across users or endpoints, and
/// must never bypass the current request's authentication.
/// </summary>
public class IdempotencyKeyMiddlewareTests : IClassFixture<MotoTestFactory>
{
    private readonly MotoTestFactory _factory;

    public IdempotencyKeyMiddlewareTests(MotoTestFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NewClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    private static HttpContent RawJson(string json) =>
        new StringContent(json, Encoding.UTF8, "application/json");

    private static async Task LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { emailOrUsername = username, password = "pass" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string> LoginAndGetUserIdAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { emailOrUsername = username, password = "pass" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("userId").GetString()!;
    }

    /// <summary>Create a published future tour as organizer1; returns (tourId, tourMotardId).</summary>
    private async Task<(string TourId, string TourMotardId)> CreatePublishedTourAsync()
    {
        var organizer = NewClient();
        await LoginAsync(organizer, "organizer1");
        var response = await organizer.PostAsync("/api/tours", RawJson(
            $"{{\"title\":\"Idempotency Test Tour {Guid.NewGuid():N}\",\"startAt\":\"{DateTime.UtcNow.AddHours(48):o}\"," +
            "\"startLocationText\":\"Paris\",\"endLocationText\":\"Rouen\",\"capacitySeats\":3,\"pricePerSeat\":10,\"publishImmediately\":true}"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (doc.RootElement.GetProperty("tourId").GetString()!,
                doc.RootElement.GetProperty("creatorTourMotardId").GetString()!);
    }

    private static string BookingBody(string tourId, string tourMotardId) =>
        $"{{\"tourId\":\"{tourId}\",\"tourMotardId\":\"{tourMotardId}\",\"emergencyContact\":\"+33 6 11 22 33 44\",\"costSharingOptIn\":false}}";

    [Fact]
    public async Task SameKey_SameUser_ReplaysCachedResponse()
    {
        var (tourId, tourMotardId) = await CreatePublishedTourAsync();
        var client = NewClient();
        var userId = await LoginAndGetUserIdAsync(client, "participant2");

        var body = BookingBody(tourId, tourMotardId);
        client.DefaultRequestHeaders.Add("Idempotency-Key", "replay-key-1");

        var first = await client.PostAsync("/api/bookings/request", RawJson(body));
        var second = await client.PostAsync("/api/bookings/request", RawJson(body));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        // The second call must be the cached copy of the first
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());

        // ...and only one booking may exist
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Bookings.CountAsync(b => b.ParticipantUserId == userId && b.TourMotardId == Guid.Parse(tourMotardId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SameKey_DifferentUser_NoCrossUserReplay()
    {
        var (tourId, tourMotardId) = await CreatePublishedTourAsync();
        var body = BookingBody(tourId, tourMotardId);

        var clientA = NewClient();
        var clientB = NewClient();
        await LoginAsync(clientA, "participant1");
        await LoginAsync(clientB, "participant2");

        clientA.DefaultRequestHeaders.Add("Idempotency-Key", "shared-key");
        clientB.DefaultRequestHeaders.Add("Idempotency-Key", "shared-key");

        var responseA = await clientA.PostAsync("/api/bookings/request", RawJson(body));
        var responseB = await clientB.PostAsync("/api/bookings/request", RawJson(body));

        Assert.Equal(HttpStatusCode.Created, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, responseB.StatusCode);

        // B must not receive A's cached response: distinct booking ids
        Assert.NotEqual(await responseA.Content.ReadAsStringAsync(), await responseB.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var motardId = Guid.Parse(tourMotardId);
        var count = await db.Bookings.CountAsync(b => b.TourMotardId == motardId);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task SameKey_DifferentEndpoint_NoWrongReplay()
    {
        var (tourId, tourMotardId) = await CreatePublishedTourAsync();
        var client = NewClient();
        await LoginAsync(client, "participant2");

        var bookingBody = BookingBody(tourId, tourMotardId);
        client.DefaultRequestHeaders.Add("Idempotency-Key", "cross-endpoint-key");
        var bookingResponse = await client.PostAsync("/api/bookings/request", RawJson(bookingBody));
        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);

        // Same key, different endpoint (organizer creates a tour): must execute
        // fresh, not replay the booking response
        var organizerClient = NewClient();
        await LoginAsync(organizerClient, "organizer1");
        organizerClient.DefaultRequestHeaders.Add("Idempotency-Key", "cross-endpoint-key");
        var tourResponse = await organizerClient.PostAsync("/api/tours", RawJson(
            $"{{\"title\":\"Cross Endpoint Tour {Guid.NewGuid():N}\",\"startAt\":\"{DateTime.UtcNow.AddHours(72):o}\"," +
            "\"startLocationText\":\"Lyon\",\"endLocationText\":\"Nice\",\"capacitySeats\":1,\"pricePerSeat\":5,\"publishImmediately\":true}"));

        Assert.Equal(HttpStatusCode.Created, tourResponse.StatusCode);
        var tourJson = await tourResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("bookingId", tourJson);
        Assert.Contains("creatorTourMotardId", tourJson);
    }

    [Fact]
    public async Task Anonymous_WithKey_IsNotCached()
    {
        var (tourId, tourMotardId) = await CreatePublishedTourAsync();
        var body = BookingBody(tourId, tourMotardId);

        var anon = NewClient();
        anon.DefaultRequestHeaders.Add("Idempotency-Key", "anon-key");
        var anonResponse = await anon.PostAsync("/api/bookings/request", RawJson(body));
        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);

        // The failed anonymous attempt must not poison the key for a real user
        var client = NewClient();
        await LoginAsync(client, "participant2");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "anon-key");
        var authedResponse = await client.PostAsync("/api/bookings/request", RawJson(body));
        Assert.Equal(HttpStatusCode.Created, authedResponse.StatusCode);
    }

    [Fact]
    public async Task InFlight_SameKey_Returns409()
    {
        var (tourId, tourMotardId) = await CreatePublishedTourAsync();
        var client = NewClient();
        var userId = await LoginAndGetUserIdAsync(client, "participant2");

        var body = BookingBody(tourId, tourMotardId);

        // Simulate a concurrent first request that claimed the key and is still executing
        var cacheKey = await IdempotencyKeyMiddleware.BuildCacheKeyAsync(
            BuildKeyContext(userId, "POST", "/api/bookings/request", "", body), userId, "inflight-key");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.IdempotencyKeys.Add(new IdempotencyKey
            {
                Key = cacheKey,
                ResponseBody = string.Empty,
                StatusCode = 0, // in-flight marker
                ContentType = "application/json",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        client.DefaultRequestHeaders.Add("Idempotency-Key", "inflight-key");
        var response = await client.PostAsync("/api/bookings/request", RawJson(body));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var verify = _factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await verifyDb.Bookings
            .AnyAsync(b => b.ParticipantUserId == userId && b.TourMotardId == Guid.Parse(tourMotardId)));
    }

    /// <summary>Build the same composite key the middleware builds, for seeding claim rows in tests.</summary>
    private static DefaultHttpContext BuildKeyContext(string userId, string method, string path, string query, string body)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth"));
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString(query);
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context;
    }
}
