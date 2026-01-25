using AutoMapper;
using ReservationService.Common.Events;
using ReservationService.Common.Events.Published;
using ReservationService.Common.Exceptions;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;
using ReservationService.DTO;
using ReservationService.Infrastructure.Clients;
using ReservationService.Repositories.Interfaces;
using ReservationService.Services.Implementations;
using ReservationService.Services.Interfaces;

namespace ReservationService.Tests.Services;

public class ReservationServiceTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IReservationRepository> _reservationRepositoryMock;
    private readonly Mock<IAccommodationClient> _accommodationClientMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly ReservationService.Services.Implementations.ReservationService _sut;

    public ReservationServiceTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _accommodationClientMock = new Mock<IAccommodationClient>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _eventBusMock = new Mock<IEventBus>();

        _sut = new ReservationService.Services.Implementations.ReservationService(
            _currentUserServiceMock.Object,
            _reservationRepositoryMock.Object,
            _accommodationClientMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _eventBusMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ShouldThrowUnauthorizedAccessException_WhenUserIdIsNull()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var request = CreateValidReservationRequest();

        // Act & Assert
        await _sut.Invoking(s => s.CreateAsync(request, Guid.NewGuid()))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowIdempotencyReplayException_WhenRequestAlreadyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _reservationRepositoryMock.Setup(x => x.ExistsByIdempotencyKey(userId, idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = CreateValidReservationRequest();

        // Act & Assert
        await _sut.Invoking(s => s.CreateAsync(request, idempotencyKey))
            .Should().ThrowAsync<IdempotencyReplayException>()
            .WithMessage("Same request found");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowPastDateException_WhenStartDateIsInPast()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _reservationRepositoryMock.Setup(x => x.ExistsByIdempotencyKey(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateReservationRequest
        {
            AccommodationId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            GuestsCount = 2
        };

        // Act & Assert
        await _sut.Invoking(s => s.CreateAsync(request, Guid.NewGuid()))
            .Should().ThrowAsync<PastDateException>()
            .WithMessage("Cannot create reservation for past dates.");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowArgumentOutOfRangeException_WhenEndDateBeforeStartDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _reservationRepositoryMock.Setup(x => x.ExistsByIdempotencyKey(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateReservationRequest
        {
            AccommodationId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            GuestsCount = 2
        };

        // Act & Assert
        await _sut.Invoking(s => s.CreateAsync(request, Guid.NewGuid()))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*End date must be after start date.*");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowMaxGuestsExceededException_WhenGuestsCountExceedsCapacity()
    {
        // Arrange
        SetupValidUser();
        var request = CreateValidReservationRequest();
        request.GuestsCount = 10;

        var accommodationInfo = new AccommodationReservationInfoResponseDTO
        {
            MaxGuests = 5,
            TotalPrice = 500m,
            HostId = Guid.NewGuid(),
            Name = "Test Accommodation",
            IsAutoAcceptEnabled = false
        };

        _reservationRepositoryMock.Setup(x => x.ExistsByIdempotencyKey(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accommodationClientMock.Setup(x => x.GetAccommodationReservationInfoAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accommodationInfo);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>());

        // Act & Assert
        await _sut.Invoking(s => s.CreateAsync(request, Guid.NewGuid()))
            .Should().ThrowAsync<MaxGuestsExceededException>()
            .WithMessage("Guests count exceeds accommodation capacity.");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflictException_WhenOverlappingApprovedReservationExists()
    {
        // Arrange
        SetupValidUser();
        var request = CreateValidReservationRequest();

        var accommodationInfo = new AccommodationReservationInfoResponseDTO
        {
            MaxGuests = 5,
            TotalPrice = 500m,
            HostId = Guid.NewGuid(),
            Name = "Test Accommodation",
            IsAutoAcceptEnabled = false
        };

        _reservationRepositoryMock.Setup(x => x.ExistsByIdempotencyKey(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accommodationClientMock.Setup(x => x.GetAccommodationReservationInfoAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accommodationInfo);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>());
        _reservationRepositoryMock.Setup(x => x.HasOverlappingApprovedReservationAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await _sut.Invoking(s => s.CreateAsync(request, Guid.NewGuid()))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("Accommodation already has an approved reservation for the selected dates.");
    }

    [Fact]
    public async Task CreateAsync_ShouldCreatePendingReservation_WhenAutoAcceptIsDisabled()
    {
        // Arrange
        SetupValidUser();
        var request = CreateValidReservationRequest();
        var idempotencyKey = Guid.NewGuid();

        var accommodationInfo = new AccommodationReservationInfoResponseDTO
        {
            MaxGuests = 5,
            TotalPrice = 500m,
            HostId = Guid.NewGuid(),
            Name = "Test Accommodation",
            IsAutoAcceptEnabled = false
        };

        SetupSuccessfulReservationCreation(accommodationInfo);

        // Act
        await _sut.CreateAsync(request, idempotencyKey);

        // Assert
        _reservationRepositoryMock.Verify(x => x.AddAsync(It.Is<Reservation>(r => 
            r.Status == ReservationStatus.Pending)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(
            It.IsAny<ReservationCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateApprovedReservation_WhenAutoAcceptIsEnabled()
    {
        // Arrange
        SetupValidUser();
        var request = CreateValidReservationRequest();

        var accommodationInfo = new AccommodationReservationInfoResponseDTO
        {
            MaxGuests = 5,
            TotalPrice = 500m,
            HostId = Guid.NewGuid(),
            Name = "Test Accommodation",
            IsAutoAcceptEnabled = true
        };

        SetupSuccessfulReservationCreation(accommodationInfo);
        _reservationRepositoryMock.Setup(x => x.GetOverlappingPendingForRejectionAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PendingToRejectInfo>());

        // Act
        await _sut.CreateAsync(request, Guid.NewGuid());

        // Assert
        _reservationRepositoryMock.Verify(x => x.AddAsync(It.Is<Reservation>(r => 
            r.Status == ReservationStatus.Approved)), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(
            It.IsAny<ReservationApprovedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(
            It.IsAny<ReservationRespondedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ApproveAsync Tests

    [Fact]
    public async Task ApproveAsync_ShouldThrowNotFoundException_WhenReservationDoesNotExist()
    {
        // Arrange
        SetupValidUser();
        var reservationId = Guid.NewGuid();
        _reservationRepositoryMock.Setup(x => x.GetByIdAsync(reservationId))
            .ReturnsAsync((Reservation?)null);

        // Act & Assert
        await _sut.Invoking(s => s.ApproveAsync(reservationId, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("Reservation not found.");
    }

    [Fact]
    public async Task ApproveAsync_ShouldApproveReservationAndPublishEvents()
    {
        // Arrange
        SetupValidUser();
        var reservation = CreateTestReservation();
        _reservationRepositoryMock.Setup(x => x.GetByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);
        _reservationRepositoryMock.Setup(x => x.HasOverlappingApprovedReservationAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _reservationRepositoryMock.Setup(x => x.GetOverlappingPendingForRejectionAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PendingToRejectInfo>());

        // Act
        await _sut.ApproveAsync(reservation.Id, CancellationToken.None);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Approved);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(
            It.IsAny<ReservationApprovedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(
            It.IsAny<ReservationRespondedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeclineAsync Tests

    [Fact]
    public async Task DeclineAsync_ShouldThrowNotFoundException_WhenReservationDoesNotExist()
    {
        // Arrange
        SetupValidUser();
        var reservationId = Guid.NewGuid();
        _reservationRepositoryMock.Setup(x => x.GetByIdAsync(reservationId))
            .ReturnsAsync((Reservation?)null);

        // Act & Assert
        await _sut.Invoking(s => s.DeclineAsync(reservationId, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("Reservation not found.");
    }

    [Fact]
    public async Task DeclineAsync_ShouldDeclineReservationAndPublishEvent()
    {
        // Arrange
        SetupValidUser();
        var reservation = CreateTestReservation();
        _reservationRepositoryMock.Setup(x => x.GetByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        // Act
        await _sut.DeclineAsync(reservation.Id, CancellationToken.None);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Rejected);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(
            It.Is<ReservationRespondedIntegrationEvent>(e => e.IsApproved == false), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_ShouldThrowNotFoundException_WhenReservationDoesNotExist()
    {
        // Arrange
        SetupValidUser();
        var reservationId = Guid.NewGuid();
        _reservationRepositoryMock.Setup(x => x.GetByIdAsync(reservationId))
            .ReturnsAsync((Reservation?)null);

        // Act & Assert
        await _sut.Invoking(s => s.CancelAsync(reservationId, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("Reservation not found");
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotGuest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var reservation = CreateTestReservation(guestId: differentUserId, status: ReservationStatus.Approved);
        _reservationRepositoryMock.Setup(x => x.GetByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        // Act & Assert
        await _sut.Invoking(s => s.CancelAsync(reservation.Id, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You don't have access to this reservation.");
    }

    [Fact]
    public async Task CancelAsync_ShouldCancelReservationAndPublishEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var reservation = CreateTestReservation(guestId: userId, status: ReservationStatus.Approved);
        _reservationRepositoryMock.Setup(x => x.GetByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        // Act
        await _sut.CancelAsync(reservation.Id, CancellationToken.None);

        // Assert
        reservation.Status.Should().Be(ReservationStatus.CancelledByGuest);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(
            It.IsAny<ReservationCanceledIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetApprovedForGuestAsync Tests

    [Fact]
    public async Task GetApprovedForGuestAsync_ShouldReturnApprovedReservations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var expectedReservations = new List<GuestApprovedReservationResponseDTO>
        {
            new() { Id = Guid.NewGuid(), AccommodationName = "Test 1" },
            new() { Id = Guid.NewGuid(), AccommodationName = "Test 2" }
        };

        _reservationRepositoryMock.Setup(x => x.GetApprovedReservationsByGuestIdAsync(
                It.IsAny<CancellationToken>(), userId))
            .ReturnsAsync(expectedReservations);

        // Act
        var result = await _sut.GetApprovedForGuestAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedReservations);
    }

    #endregion

    #region GetPendingForHostAsync Tests

    [Fact]
    public async Task GetPendingForHostAsync_ShouldReturnPendingReservations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var expectedReservations = new List<HostPendingReservationResponseDTO>
        {
            new() { ReservationId = Guid.NewGuid(), AccommodationName = "Test 1" },
            new() { ReservationId = Guid.NewGuid(), AccommodationName = "Test 2" }
        };

        _reservationRepositoryMock.Setup(x => x.GetPendingReservationsByHostIdAsync(
                It.IsAny<CancellationToken>(), userId))
            .ReturnsAsync(expectedReservations);

        // Act
        var result = await _sut.GetPendingForHostAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedReservations);
    }

    #endregion

    #region GetGuestDeletionEligibilityAsync Tests

    [Fact]
    public async Task GetGuestDeletionEligibilityAsync_ShouldReturnTrue_WhenNoActiveReservations()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        _reservationRepositoryMock.Setup(x => x.GuestHasActiveReservationAsync(guestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.GetGuestDeletionEligibilityAsync(guestId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetGuestDeletionEligibilityAsync_ShouldReturnFalse_WhenHasActiveReservations()
    {
        // Arrange
        var guestId = Guid.NewGuid();
        _reservationRepositoryMock.Setup(x => x.GuestHasActiveReservationAsync(guestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.GetGuestDeletionEligibilityAsync(guestId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetHostDeletionEligibilityAsync Tests

    [Fact]
    public async Task GetHostDeletionEligibilityAsync_ShouldReturnTrue_WhenNoActiveReservations()
    {
        // Arrange
        var hostId = Guid.NewGuid();
        _reservationRepositoryMock.Setup(x => x.HostHasActiveReservationAsync(hostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.GetHostDeletionEligibilityAsync(hostId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetHostDeletionEligibilityAsync_ShouldReturnFalse_WhenHasActiveReservations()
    {
        // Arrange
        var hostId = Guid.NewGuid();
        _reservationRepositoryMock.Setup(x => x.HostHasActiveReservationAsync(hostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.GetHostDeletionEligibilityAsync(hostId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private void SetupValidUser()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.Email).Returns("test@example.com");
        _currentUserServiceMock.Setup(x => x.Username).Returns("testuser");
    }

    private CreateReservationRequest CreateValidReservationRequest()
    {
        return new CreateReservationRequest
        {
            AccommodationId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
            GuestsCount = 2
        };
    }

    private void SetupSuccessfulReservationCreation(AccommodationReservationInfoResponseDTO accommodationInfo)
    {
        _reservationRepositoryMock.Setup(x => x.ExistsByIdempotencyKey(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _accommodationClientMock.Setup(x => x.GetAccommodationReservationInfoAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accommodationInfo);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>());
        _reservationRepositoryMock.Setup(x => x.HasOverlappingApprovedReservationAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private Reservation CreateTestReservation(
        Guid? guestId = null,
        ReservationStatus status = ReservationStatus.Pending)
    {
        return new Reservation(
            accommodationId: Guid.NewGuid(),
            guestId: guestId ?? Guid.NewGuid(),
            hostId: Guid.NewGuid(),
            accommodationName: "Test Accommodation",
            guestEmail: "test@example.com",
            guestUsername: "testuser",
            startDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
            guestsCount: 2,
            totalPrice: 500m,
            status: status,
            idempotencyKey: Guid.NewGuid());
    }

    #endregion
}
