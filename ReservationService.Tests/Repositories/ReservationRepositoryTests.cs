using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;
using ReservationService.Repositories.Implementations;

namespace ReservationService.Tests.Repositories;

public class ReservationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ReservationRepository _repository;

    public ReservationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ReservationRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddReservationToContext()
    {
        // Arrange
        var reservation = CreateTestReservation();

        // Act
        await _repository.AddAsync(reservation);
        await _context.SaveChangesAsync();

        // Assert
        var savedReservation = await _context.Reservations.FindAsync(reservation.Id);
        savedReservation.Should().NotBeNull();
        savedReservation!.GuestId.Should().Be(reservation.GuestId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnReservation_WhenExists()
    {
        // Arrange
        var reservation = CreateTestReservation();
        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(reservation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(reservation.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Remove_ShouldRemoveReservation()
    {
        // Arrange
        var reservation = CreateTestReservation();
        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();

        // Act
        _repository.Remove(reservation);
        await _context.SaveChangesAsync();

        // Assert
        var removedReservation = await _context.Reservations.FindAsync(reservation.Id);
        removedReservation.Should().BeNull();
    }

    [Fact]
    public async Task HasOverlappingApprovedReservationAsync_ShouldReturnTrue_WhenOverlapExists()
    {
        // Arrange
        var accommodationId = Guid.NewGuid();
        var startDate = new DateOnly(2026, 2, 1);
        var endDate = new DateOnly(2026, 2, 10);

        var existingReservation = CreateTestReservation(
            accommodationId: accommodationId,
            startDate: new DateOnly(2026, 2, 5),
            endDate: new DateOnly(2026, 2, 15),
            status: ReservationStatus.Approved);

        await _context.Reservations.AddAsync(existingReservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasOverlappingApprovedReservationAsync(
            accommodationId, startDate, endDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasOverlappingApprovedReservationAsync_ShouldReturnFalse_WhenNoOverlap()
    {
        // Arrange
        var accommodationId = Guid.NewGuid();
        var startDate = new DateOnly(2026, 2, 1);
        var endDate = new DateOnly(2026, 2, 10);

        var existingReservation = CreateTestReservation(
            accommodationId: accommodationId,
            startDate: new DateOnly(2026, 3, 1),
            endDate: new DateOnly(2026, 3, 10),
            status: ReservationStatus.Approved);

        await _context.Reservations.AddAsync(existingReservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasOverlappingApprovedReservationAsync(
            accommodationId, startDate, endDate);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasOverlappingApprovedReservationAsync_ShouldReturnFalse_WhenOverlapExistsButNotApproved()
    {
        // Arrange
        var accommodationId = Guid.NewGuid();
        var startDate = new DateOnly(2026, 2, 1);
        var endDate = new DateOnly(2026, 2, 10);

        var existingReservation = CreateTestReservation(
            accommodationId: accommodationId,
            startDate: new DateOnly(2026, 2, 5),
            endDate: new DateOnly(2026, 2, 15),
            status: ReservationStatus.Pending);

        await _context.Reservations.AddAsync(existingReservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HasOverlappingApprovedReservationAsync(
            accommodationId, startDate, endDate);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByIdempotencyKey_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();

        var reservation = CreateTestReservation(
            guestId: guestId,
            idempotencyKey: idempotencyKey);

        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByIdempotencyKey(guestId, idempotencyKey);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByIdempotencyKey_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsByIdempotencyKey(guestId, idempotencyKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetApprovedReservationsByGuestIdAsync_ShouldReturnOnlyApprovedReservations()
    {
        // Arrange
        var guestId = Guid.NewGuid();

        var approvedReservation1 = CreateTestReservation(guestId: guestId, status: ReservationStatus.Approved);
        var approvedReservation2 = CreateTestReservation(guestId: guestId, status: ReservationStatus.Approved);
        var pendingReservation = CreateTestReservation(guestId: guestId, status: ReservationStatus.Pending);

        await _context.Reservations.AddRangeAsync(approvedReservation1, approvedReservation2, pendingReservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetApprovedReservationsByGuestIdAsync(CancellationToken.None, guestId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.Id == approvedReservation1.Id || r.Id == approvedReservation2.Id);
    }

    [Fact]
    public async Task GetPendingReservationsByHostIdAsync_ShouldReturnOnlyPendingReservations()
    {
        // Arrange
        var hostId = Guid.NewGuid();

        var pendingReservation1 = CreateTestReservation(hostId: hostId, status: ReservationStatus.Pending);
        var pendingReservation2 = CreateTestReservation(hostId: hostId, status: ReservationStatus.Pending);
        var approvedReservation = CreateTestReservation(hostId: hostId, status: ReservationStatus.Approved);

        await _context.Reservations.AddRangeAsync(pendingReservation1, pendingReservation2, approvedReservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingReservationsByHostIdAsync(CancellationToken.None, hostId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.ReservationId == pendingReservation1.Id || r.ReservationId == pendingReservation2.Id);
    }

    [Fact]
    public async Task GuestHasActiveReservationAsync_ShouldReturnTrue_WhenHasApprovedFutureReservation()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        var reservation = CreateTestReservation(
            guestId: guestId,
            startDate: futureDate,
            endDate: futureDate.AddDays(5),
            status: ReservationStatus.Approved);

        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GuestHasActiveReservationAsync(guestId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GuestHasActiveReservationAsync_ShouldReturnFalse_WhenHasOnlyPastReservations()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));

        var reservation = CreateTestReservation(
            guestId: guestId,
            startDate: pastDate,
            endDate: pastDate.AddDays(5),
            status: ReservationStatus.Approved);

        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GuestHasActiveReservationAsync(guestId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HostHasActiveReservationAsync_ShouldReturnTrue_WhenHasPendingFutureReservation()
    {
        // Arrange
        var hostId = Guid.NewGuid();
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        var reservation = CreateTestReservation(
            hostId: hostId,
            startDate: futureDate,
            endDate: futureDate.AddDays(5),
            status: ReservationStatus.Pending);

        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.HostHasActiveReservationAsync(hostId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetOverlappingPendingForRejectionAsync_ShouldReturnOverlappingPendingReservations()
    {
        // Arrange
        var accommodationId = Guid.NewGuid();
        var startDate = new DateOnly(2026, 2, 1);
        var endDate = new DateOnly(2026, 2, 10);

        var overlappingPending1 = CreateTestReservation(
            accommodationId: accommodationId,
            startDate: new DateOnly(2026, 2, 5),
            endDate: new DateOnly(2026, 2, 15),
            status: ReservationStatus.Pending);

        var overlappingPending2 = CreateTestReservation(
            accommodationId: accommodationId,
            startDate: new DateOnly(2026, 1, 28),
            endDate: new DateOnly(2026, 2, 3),
            status: ReservationStatus.Pending);

        var nonOverlapping = CreateTestReservation(
            accommodationId: accommodationId,
            startDate: new DateOnly(2026, 3, 1),
            endDate: new DateOnly(2026, 3, 10),
            status: ReservationStatus.Pending);

        await _context.Reservations.AddRangeAsync(overlappingPending1, overlappingPending2, nonOverlapping);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetOverlappingPendingForRejectionAsync(
            accommodationId, startDate, endDate, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.ReservationId == overlappingPending1.Id);
        result.Should().Contain(r => r.ReservationId == overlappingPending2.Id);
    }

    [Fact]
    public async Task Query_ShouldReturnQueryableOfReservations()
    {
        // Arrange
        var reservation1 = CreateTestReservation();
        var reservation2 = CreateTestReservation();

        await _context.Reservations.AddRangeAsync(reservation1, reservation2);
        await _context.SaveChangesAsync();

        // Act
        var query = _repository.Query();
        var result = await query.ToListAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    private Reservation CreateTestReservation(
        Guid? guestId = null,
        Guid? hostId = null,
        Guid? accommodationId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        ReservationStatus status = ReservationStatus.Pending,
        Guid? idempotencyKey = null)
    {
        var start = startDate ?? new DateOnly(2026, 2, 1);
        var end = endDate ?? new DateOnly(2026, 2, 10);

        return new Reservation(
            accommodationId: accommodationId ?? Guid.NewGuid(),
            guestId: guestId ?? Guid.NewGuid(),
            hostId: hostId ?? Guid.NewGuid(),
            accommodationName: "Test Accommodation",
            guestEmail: "test@example.com",
            guestUsername: "testuser",
            startDate: start,
            endDate: end,
            guestsCount: 2,
            totalPrice: 100.00m,
            status: status,
            idempotencyKey: idempotencyKey ?? Guid.NewGuid());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
