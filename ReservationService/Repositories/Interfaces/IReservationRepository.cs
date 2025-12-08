using ReservationService.Domain.Entities;
using ReservationService.DTO;

namespace ReservationService.Repositories.Interfaces
{
	public interface IReservationRepository : IRepository<Reservation>
	{
		Task<int> RejectOverlappingPendingAsync(Guid accommodationId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct);
		Task<bool> HasOverlappingApprovedReservationAsync(Guid accommodationId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default);
		Task AcquireAccommodationLockAsync(Guid accommodationId, CancellationToken ct = default);
		Task<bool> ExistsByIdempotencyKey(Guid guestId, Guid idempotencyKey, CancellationToken ct = default);
		Task<IReadOnlyList<GuestApprovedReservationResponseDTO>> GetApprovedReservationsByGuestIdAsync(CancellationToken ct, Guid GuestId);
		Task<bool> GuestHasActiveReservationAsync(Guid guestId, CancellationToken ct);
		Task<bool> HostHasActiveReservationAsync(Guid hostId, CancellationToken ct);
	}
}
