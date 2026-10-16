using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;
using moto_micro_tours.Domain.Enums;
using Xunit;

namespace moto_micro_tours.Tests;

/// <summary>
/// Unauthorized-access tests for the payment endpoint (P0 fix).
/// Payment may only be triggered by the booking's participant.
/// Runs against the real API with an isolated SQLite database.
/// </summary>
public class PaymentAuthorizationTests : IClassFixture<MotoTestFactory>
{
    private readonly MotoTestFactory _factory;

    public PaymentAuthorizationTests(MotoTestFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NewClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    private static HttpContent JsonBody(object body) =>
        new StringContent(System.Text.Json.JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8, "application/json");

    /// <summary>
    /// Create a published future tour by organizer1 and an approved booking for
    /// participant2 on it. Returns the booking id.
    /// </summary>
    private async Task<Guid> CreateApprovedBookingAsync(string title)
    {
        var organizer = NewClient();
        await LoginAsync(organizer, "organizer1");

        var createResponse = await organizer.PostAsync("/api/tours", JsonBody(new
        {
            title,
            startAt = DateTime.UtcNow.AddHours(48),
            startLocationText = "Paris",
            endLocationText = "Rouen",
            capacitySeats = 2,
            pricePerSeat = 15m,
            publishImmediately = true
        }));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTourResponse>();
        Assert.NotNull(created);

        var participant = NewClient();
        await LoginAsync(participant, "participant2");

        var bookingResponse = await participant.PostAsync("/api/bookings/request", JsonBody(new
        {
            tourId = created!.TourId,
            tourMotardId = created.CreatorTourMotardId,
            emergencyContact = "+33 6 11 22 33 44",
            costSharingOptIn = false
        }));
        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal("Pending", booking!.Status);

        var approveResponse = await organizer.PostAsync($"/api/bookings/{booking.BookingId}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        return booking.BookingId;
    }

    private static async Task LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { emailOrUsername = username, password = "pass" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Pay_WithoutLogin_ReturnsUnauthorized()
    {
        var client = NewClient();
        var response = await client.PostAsync($"/api/payments/pay/{Guid.NewGuid()}", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Pay_OtherUsersBooking_ReturnsForbidden()
    {
        var bookingId = await CreateApprovedBookingAsync("Auth Test Tour - Forbidden");

        // participant1 is logged in but does not own the booking
        var stranger = NewClient();
        await LoginAsync(stranger, "participant1");

        var response = await stranger.PostAsync($"/api/payments/pay/{bookingId}", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pay_OwnBooking_Succeeds()
    {
        var bookingId = await CreateApprovedBookingAsync("Auth Test Tour - Owner Pays");

        var owner = NewClient();
        await LoginAsync(owner, "participant2");

        var response = await owner.PostAsync($"/api/payments/pay/{bookingId}", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var confirmation = await response.Content.ReadFromJsonAsync<PaymentConfirmation>();
        Assert.NotNull(confirmation);
        Assert.True(confirmation!.Success);
        Assert.Equal("Paid", confirmation.BookingStatus);
    }

    [Fact]
    public async Task Cancel_PaidBooking_MarksPayoutFailed_LikeRefundPath()
    {
        var bookingId = await CreateApprovedBookingAsync("Auth Test Tour - Cancel Payout");

        var owner = NewClient();
        await LoginAsync(owner, "participant2");

        var payResponse = await owner.PostAsync($"/api/payments/pay/{bookingId}", null);
        Assert.Equal(HttpStatusCode.OK, payResponse.StatusCode);

        // >24h before start, so the participant may cancel with a full refund
        var cancelResponse = await owner.PostAsync($"/api/bookings/{bookingId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        // The payout must not stay Pending after a refund (same as the motard refund path)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings
            .Include(b => b.Payment)
            .Include(b => b.Payout)
            .Include(b => b.TourMotard)
            .FirstAsync(b => b.Id == bookingId);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(PaymentStatus.Refunded, booking.Payment!.Status);
        Assert.Equal(PayoutStatus.Failed, booking.Payout!.Status);
        Assert.Equal(0, booking.TourMotard!.SeatsBooked);
    }

    // Response DTOs (case-insensitive JSON binding)
    private class CreateTourResponse
    {
        public Guid TourId { get; set; }
        public Guid CreatorTourMotardId { get; set; }
        public string Status { get; set; } = "";
    }

    private class BookingResponse
    {
        public Guid BookingId { get; set; }
        public string Status { get; set; } = "";
    }

    private class PaymentConfirmation
    {
        public bool Success { get; set; }
        public string BookingStatus { get; set; } = "";
    }
}
