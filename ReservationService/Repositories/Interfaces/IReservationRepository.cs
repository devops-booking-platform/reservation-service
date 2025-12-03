using ReservationService.Domain.Entities;

namespace ReservationService.Repositories.Interfaces
{
	public interface IReservationRepository : IRepository<Reservation>
	{
		Task<int> RejectOverlappingPendingAsync(Guid accommodationId, DateOnly startDate, DateOnly endDate, CancellationToken ct);
		Task<bool> HasOverlappingApprovedReservationAsync(Guid accommodationId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
		Task AcquireAccommodationLockAsync(Guid accommodationId, CancellationToken ct = default);
		Task<bool> ExistsByIdempotencyKey(Guid guestId, Guid idempotencyKey, CancellationToken ct = default);
	}
}
