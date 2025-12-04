using ReservationService.DTO;

namespace ReservationService.Services.Interfaces
{
	public interface IReservationService
	{
		Task CreateAsync(CreateReservationRequestDTO reservationRequest, Guid idempotencyKey, CancellationToken ct = default);
	}
}
