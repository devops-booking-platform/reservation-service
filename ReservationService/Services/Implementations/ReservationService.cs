using Microsoft.EntityFrameworkCore;
using ReservationService.Common.Exceptions;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;
using ReservationService.DTO;
using ReservationService.Infrastructure.Clients;
using ReservationService.Repositories.Interfaces;
using ReservationService.Services.Interfaces;

namespace ReservationService.Services.Implementations;

public class ReservationService(
    ICurrentUserService currentUserService,
    IReservationRepository reservationRepository,
    IAccommodationClient accommodationClient,
    IUnitOfWork unitOfWork) : IReservationService
{
    public async Task CreateAsync(CreateReservationRequest request, Guid idempotencyKey,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserIdOrThrow();

        await EnsureNotReplayedAsync(userId, idempotencyKey, ct);

        ValidateRequest(request);

        var accommodationReservationInfo = await GetReservationInfoAsync(request, ct);

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        await LockAccommodationAsync(request.AccommodationId, ct);

        EnsureGuestsWithinCapacity(request.GuestsCount, accommodationReservationInfo.MaxGuests);

        await EnsureNoApprovedOverlapAsync(request.AccommodationId, request.StartDate, request.EndDate, ct);

        var status = accommodationReservationInfo.IsAutoAcceptEnabled
            ? ReservationStatus.Approved
            : ReservationStatus.Pending;

        var reservation = BuildReservation(userId, idempotencyKey, request, accommodationReservationInfo, status);

        await reservationRepository.AddAsync(reservation);

        await SaveChangesHandlingIdempotencyAsync(ct);

        await RejectPendingIfApprovedAsync(status, request, ct);

        await transaction.CommitAsync(ct);
    }

    private Guid GetCurrentUserIdOrThrow()
        => currentUserService.UserId ?? throw new UnauthorizedAccessException();

    private async Task EnsureNotReplayedAsync(Guid userId, Guid idempotencyKey, CancellationToken ct)
    {
        if (await reservationRepository.ExistsByIdempotencyKey(userId, idempotencyKey, ct))
            throw new IdempotencyReplayException("Same request found");
    }

    private static void ValidateRequest(CreateReservationRequest request)
    {
        var todayUtc = DateTime.UtcNow;

        if (request.StartDate < todayUtc)
            throw new PastDateException("Cannot create reservation for past dates.");

        if (request.StartDate >= request.EndDate)
            throw new ArgumentOutOfRangeException(nameof(request.EndDate),
                "End date must be after start date.");

        if (request.GuestsCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.GuestsCount),
                "Guests count must be a positive number.");
    }

    private Task<AccommodationReservationInfoResponseDTO> GetReservationInfoAsync(
        CreateReservationRequest request, CancellationToken ct)
        => accommodationClient.GetAccommodationReservationInfoAsync(
            request.AccommodationId,
            request.StartDate,
            request.EndDate,
            request.GuestsCount,
            ct);

    private async Task LockAccommodationAsync(Guid accommodationId, CancellationToken ct)
    {
        try
        {
            await reservationRepository.AcquireAccommodationLockAsync(accommodationId, ct);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 50001)
        {
            throw new ConflictException("Reservation is being processed. Please try again.");
        }
    }

    private static void EnsureGuestsWithinCapacity(int guestsCount, int maxGuests)
    {
        if (guestsCount > maxGuests)
            throw new MaxGuestsExceededException("Guests count exceeds accommodation capacity.");
    }

    private Reservation BuildReservation(
        Guid userId,
        Guid idempotencyKey,
        CreateReservationRequest request,
        AccommodationReservationInfoResponseDTO info,
        ReservationStatus status)
    {
        return new Reservation(
            accommodationId: request.AccommodationId,
            guestId: userId,
            hostId: info.HostId,
            idempotencyKey: idempotencyKey,
            accommodationName: info.Name,
            guestEmail: currentUserService.Email ?? throw new InvalidOperationException("Email missing in token."),
            guestUsername: currentUserService.Username ??
                           throw new InvalidOperationException("Username missing in token."),
            startDate: request.StartDate,
            endDate: request.EndDate,
            guestsCount: request.GuestsCount,
            totalPrice: info.TotalPrice,
            status: status
        );
    }

    private async Task SaveChangesHandlingIdempotencyAsync(CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                                           && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            throw new IdempotencyReplayException("Same request found");
        }
    }

    private Task RejectPendingIfApprovedAsync(ReservationStatus status, CreateReservationRequest request,
        CancellationToken ct)
    {
        if (status != ReservationStatus.Approved)
            return Task.CompletedTask;

        return reservationRepository.RejectOverlappingPendingAsync(
            request.AccommodationId,
            request.StartDate,
            request.EndDate,
            ct);
    }

    private async Task EnsureNoApprovedOverlapAsync(Guid accommodationId, DateTimeOffset startDate,
        DateTimeOffset endDate, CancellationToken ct = default)
    {
        var hasOverlap =
            await reservationRepository.HasOverlappingApprovedReservationAsync(accommodationId, startDate, endDate,
                ct);
        if (hasOverlap)
            throw new ConflictException(
                "Accommodation already has an approved reservation for the selected dates.");
    }

    public async Task<IReadOnlyList<GuestApprovedReservationResponseDTO>> GetApprovedForGuestAsync(
        CancellationToken ct)
    {
        var guestId = GetCurrentUserIdOrThrow();

        var approvedReservations = await reservationRepository
            .GetApprovedReservationsByGuestIdAsync(ct, guestId);

        return approvedReservations;
    }

    public async Task ApproveAsync(Guid reservationId, CancellationToken ct)
    {
        GetCurrentUserIdOrThrow();

        var reservation = await reservationRepository.GetByIdAsync(reservationId);

        if (reservation == null)
        {
            throw new NotFoundException("Reservation not found.");
        }

        await EnsureNoApprovedOverlapAsync(reservation.AccommodationId, reservation.StartDate, reservation.EndDate,
            ct);

        reservation.Approve();

        await unitOfWork.SaveChangesAsync(ct);

        await reservationRepository.RejectOverlappingPendingAsync(
            reservation.AccommodationId,
            reservation.StartDate,
            reservation.EndDate,
            ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeclineAsync(Guid reservationId, CancellationToken ct)
    {
        GetCurrentUserIdOrThrow();

        var reservation = await reservationRepository.GetByIdAsync(reservationId);

        if (reservation == null)
        {
            throw new NotFoundException("Reservation not found.");
        }

        reservation.Decline();

        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid reservationId, CancellationToken ct)
    {
        var guestId = GetCurrentUserIdOrThrow();

        var reservation = await reservationRepository.GetByIdAsync(reservationId);
        ValidateCancellationRequest(reservation, guestId);

        reservation!.Cancel();

        await unitOfWork.SaveChangesAsync(ct);
    }

    private static void ValidateCancellationRequest(Reservation? reservation, Guid guestId)
    {
        if (reservation is null)
            throw new NotFoundException("Reservation not found");

        if (reservation.Status == ReservationStatus.Rejected)
            throw new UnauthorizedAccessException("This reservation has been rejected.");

        if (reservation.GuestId != guestId)
            throw new UnauthorizedAccessException("You don't have access to this reservation.");
    }
}