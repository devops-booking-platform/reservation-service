using ReservationService.DTO;

namespace ReservationService.Services.Interfaces
{
	public interface IReservationService
	{
		Task CancelAsync(Guid reservationId, CancellationToken ct = default);
		Task CreateAsync(CreateReservationRequest reservationRequest, Guid idempotencyKey, CancellationToken ct = default);
		Task<IReadOnlyList<GuestApprovedReservationResponseDTO>> GetApprovedForGuestAsync(CancellationToken ct);
		Task ApproveAsync(Guid reservationId, CancellationToken ct);
		Task DeclineAsync(Guid reservationId, CancellationToken ct);
		Task<bool> GetGuestDeletionEligibilityAsync(Guid guestId, CancellationToken ct);
		Task<bool> GetHostDeletionEligibilityAsync(Guid hostId, CancellationToken ct);
	}
}
