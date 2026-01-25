using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Domain.Entities;
using ReservationService.Repositories.Implementations;

namespace ReservationService.Tests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Repository<Reservation> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new Repository<Reservation>(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntityToContext()
    {
        // Arrange
        var entity = CreateTestReservation();

        // Act
        await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Assert
        var savedEntity = await _context.Reservations.FindAsync(entity.Id);
        savedEntity.Should().NotBeNull();
        savedEntity!.Id.Should().Be(entity.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenExists()
    {
        // Arrange
        var entity = CreateTestReservation();
        await _context.Reservations.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
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
    public async Task Remove_ShouldRemoveEntityFromContext()
    {
        // Arrange
        var entity = CreateTestReservation();
        await _context.Reservations.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        _repository.Remove(entity);
        await _context.SaveChangesAsync();

        // Assert
        var removedEntity = await _context.Reservations.FindAsync(entity.Id);
        removedEntity.Should().BeNull();
    }

    [Fact]
    public void Query_ShouldReturnIQueryable()
    {
        // Act
        var query = _repository.Query();

        // Assert
        query.Should().NotBeNull();
        query.Should().BeAssignableTo<IQueryable<Reservation>>();
    }

    [Fact]
    public async Task Query_ShouldReturnAllEntities()
    {
        // Arrange
        var entity1 = CreateTestReservation();
        var entity2 = CreateTestReservation();
        await _context.Reservations.AddRangeAsync(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var query = _repository.Query();
        var result = await query.ToListAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Id == entity1.Id);
        result.Should().Contain(e => e.Id == entity2.Id);
    }

    [Fact]
    public async Task Query_ShouldAllowFiltering()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        var entity1 = CreateTestReservation(guestId: guestId);
        var entity2 = CreateTestReservation();
        await _context.Reservations.AddRangeAsync(entity1, entity2);
        await _context.SaveChangesAsync();

        // Act
        var query = _repository.Query().Where(r => r.GuestId == guestId);
        var result = await query.ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(entity1.Id);
    }

    private Reservation CreateTestReservation(Guid? guestId = null)
    {
        return new Reservation(
            accommodationId: Guid.NewGuid(),
            guestId: guestId ?? Guid.NewGuid(),
            hostId: Guid.NewGuid(),
            accommodationName: "Test Accommodation",
            guestEmail: "test@example.com",
            guestUsername: "testuser",
            startDate: new DateOnly(2026, 2, 1),
            endDate: new DateOnly(2026, 2, 10),
            guestsCount: 2,
            totalPrice: 100.00m,
            status: Domain.Enums.ReservationStatus.Pending,
            idempotencyKey: Guid.NewGuid());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
