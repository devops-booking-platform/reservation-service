using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservationService.Data;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;
using ReservationService.DTO;

namespace ReservationService.Tests.Integration;

public class ReservationControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _context;
    private readonly IServiceScope _scope;

    public ReservationControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        
        // Create a client with test authentication configured
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.AuthenticationScheme, options => { });
            });
        }).CreateClient();

        // Create a scope to get the database context
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Clear any existing data
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
    }

    #region Create Reservation Tests

    [Fact]
    public async Task Create_ShouldReturn400_WhenIdempotencyKeyMissing()
    {
        // Arrange
        SetAuthHeaders(Guid.NewGuid(), "Guest", "guestuser");
        
        var request = new CreateReservationRequest
        {
            AccommodationId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
            GuestsCount = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn401_WhenUnauthorized()
    {
        // Arrange - No auth headers set
        var request = new CreateReservationRequest
        {
            AccommodationId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
            GuestsCount = 2
        };

        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn409_WhenIdempotencyKeyReused()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        var accommodationId = Guid.NewGuid();

        // Create first reservation
        var existingReservation = new Reservation(
            accommodationId: accommodationId,
            guestId: guestId,
            hostId: Guid.NewGuid(),
            accommodationName: "Test Accommodation",
            guestEmail: "guest@test.com",
            guestUsername: "guestuser",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
            guestsCount: 2,
            totalPrice: 500m,
            status: ReservationStatus.Pending,
            idempotencyKey: idempotencyKey);

        await _context.Reservations.AddAsync(existingReservation);
        await _context.SaveChangesAsync();

        SetAuthHeaders(guestId, "Guest", "guestuser");
        
        var request = new CreateReservationRequest
        {
            AccommodationId = accommodationId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
            GuestsCount = 2
        };

        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey.ToString());

        // Act
        var response = await _client.PostAsJsonAsync("/api/reservations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region Get Approved Reservations Tests

    [Fact]
    public async Task GetApproved_ShouldReturn200WithReservations_WhenGuestHasApprovedReservations()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        SetAuthHeaders(guestId, "Guest", "guestuser");

        var reservation1 = CreateTestReservation(guestId: guestId, status: ReservationStatus.Approved);
        var reservation2 = CreateTestReservation(guestId: guestId, status: ReservationStatus.Approved);
        var pendingReservation = CreateTestReservation(guestId: guestId, status: ReservationStatus.Pending);

        await _context.Reservations.AddRangeAsync(reservation1, reservation2, pendingReservation);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/reservations/approved");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GuestApprovedReservationResponseDTO>>();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetApproved_ShouldReturn401_WhenUnauthorized()
    {
        // Arrange - No auth headers set
        
        // Act
        var response = await _client.GetAsync("/api/reservations/approved");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Pending Reservations Tests

    [Fact]
    public async Task GetPending_ShouldReturn200WithReservations_WhenHostHasPendingReservations()
    {
        // Arrange
        var hostId = Guid.NewGuid();
        SetAuthHeaders(hostId, "Host", "hostuser");

        var reservation1 = CreateTestReservation(hostId: hostId, status: ReservationStatus.Pending);
        var reservation2 = CreateTestReservation(hostId: hostId, status: ReservationStatus.Pending);
        var approvedReservation = CreateTestReservation(hostId: hostId, status: ReservationStatus.Approved);

        await _context.Reservations.AddRangeAsync(reservation1, reservation2, approvedReservation);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/reservations/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<HostPendingReservationResponseDTO>>();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPending_ShouldReturn401_WhenUnauthorized()
    {
        // Arrange - No auth headers set
        
        // Act
        var response = await _client.GetAsync("/api/reservations/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Cancel Reservation Tests

    [Fact]
    public async Task Cancel_ShouldReturn204_WhenValidRequest()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        SetAuthHeaders(guestId, "Guest", "guestuser");

        var reservation = CreateTestReservation(
            guestId: guestId, 
            status: ReservationStatus.Approved,
            startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.PatchAsync($"/api/reservations/{reservation.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Detach and reload from database
        _context.Entry(reservation).State = EntityState.Detached;
        var updatedReservation = await _context.Reservations.FindAsync(reservation.Id);
        updatedReservation!.Status.Should().Be(ReservationStatus.CancelledByGuest);
    }

    [Fact]
    public async Task Cancel_ShouldReturn404_WhenReservationNotFound()
    {
        // Arrange
        SetAuthHeaders(Guid.NewGuid(), "Guest", "guestuser");

        // Act
        var response = await _client.PatchAsync($"/api/reservations/{Guid.NewGuid()}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_ShouldReturn401_WhenUnauthorized()
    {
        // Act
        var response = await _client.PatchAsync($"/api/reservations/{Guid.NewGuid()}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Approve Reservation Tests

    [Fact]
    public async Task Approve_ShouldReturn404_WhenReservationNotFound()
    {
        // Arrange
        SetAuthHeaders(Guid.NewGuid(), "Host", "hostuser");

        // Act
        var response = await _client.PostAsync($"/api/reservations/approve/{Guid.NewGuid()}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Decline Reservation Tests

    [Fact]
    public async Task Decline_ShouldReturn202_WhenValidRequest()
    {
        // Arrange
        var hostId = Guid.NewGuid();
        SetAuthHeaders(hostId, "Host", "hostuser");
        var reservation = CreateTestReservation(hostId: hostId, status: ReservationStatus.Pending);

        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();
        
        // Act
        var response = await _client.PostAsync($"/api/reservations/decline/{reservation.Id}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        
        // Detach and reload from database
        _context.Entry(reservation).State = EntityState.Detached;
        var updatedReservation = await _context.Reservations.FindAsync(reservation.Id);
        updatedReservation!.Status.Should().Be(ReservationStatus.Rejected);
    }

    [Fact]
    public async Task Decline_ShouldReturn404_WhenReservationNotFound()
    {
        // Arrange
        SetAuthHeaders(Guid.NewGuid(), "Host", "hostuser");
        // Act
        var response = await _client.PostAsync($"/api/reservations/decline/{Guid.NewGuid()}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Deletion Eligibility Tests

    [Fact]
    public async Task HostDeletionEligibility_ShouldReturn200WithTrue_WhenNoActiveReservations()
    {
        // Arrange
        var hostId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/reservations/internal/deletion-eligibility/host/{hostId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<bool>();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HostDeletionEligibility_ShouldReturn200WithFalse_WhenHasActiveReservations()
    {
        // Arrange
        var hostId = Guid.NewGuid();
        var futureReservation = CreateTestReservation(
            hostId: hostId,
            status: ReservationStatus.Approved,
            startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

        await _context.Reservations.AddAsync(futureReservation);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/reservations/internal/deletion-eligibility/host/{hostId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<bool>();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GuestDeletionEligibility_ShouldReturn200WithTrue_WhenNoActiveReservations()
    {
        // Arrange
        var guestId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/reservations/internal/deletion-eligibility/guest/{guestId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<bool>();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GuestDeletionEligibility_ShouldReturn200WithFalse_WhenHasActiveReservations()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var futureReservation = CreateTestReservation(
            guestId: guestId,
            status: ReservationStatus.Approved,
            startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)));

        await _context.Reservations.AddAsync(futureReservation);
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/reservations/internal/deletion-eligibility/guest/{guestId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<bool>();
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private void SetAuthHeaders(Guid userId, string role, string username, string firstName = "Test", string lastName = "User")
    {
        _client.DefaultRequestHeaders.Remove("X-Test-UserId");
        _client.DefaultRequestHeaders.Remove("X-Test-Role");
        _client.DefaultRequestHeaders.Remove("X-Test-Username");
        _client.DefaultRequestHeaders.Remove("X-Test-FirstName");
        _client.DefaultRequestHeaders.Remove("X-Test-LastName");
        
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Role", role);
        _client.DefaultRequestHeaders.Add("X-Test-Username", username);
        _client.DefaultRequestHeaders.Add("X-Test-FirstName", firstName);
        _client.DefaultRequestHeaders.Add("X-Test-LastName", lastName);
    }

    private Reservation CreateTestReservation(
        Guid? guestId = null,
        Guid? hostId = null,
        ReservationStatus status = ReservationStatus.Pending,
        DateOnly? startDate = null)
    {
        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        return new Reservation(
            accommodationId: Guid.NewGuid(),
            guestId: guestId ?? Guid.NewGuid(),
            hostId: hostId ?? Guid.NewGuid(),
            accommodationName: "Test Accommodation",
            guestEmail: "test@example.com",
            guestUsername: "testuser",
            startDate: start,
            endDate: start.AddDays(5),
            guestsCount: 2,
            totalPrice: 500m,
            status: status,
            idempotencyKey: Guid.NewGuid());
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _scope.Dispose();
        _client.Dispose();
    }
}
