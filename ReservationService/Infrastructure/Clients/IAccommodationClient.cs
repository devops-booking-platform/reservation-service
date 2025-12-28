using ReservationService.DTO;

namespace ReservationService.Infrastructure.Clients
{
	public interface IAccommodationClient
	{
		Task<AccommodationReservationInfoResponseDTO> GetAccommodationReservationInfoAsync(Guid id, DateOnly start, DateOnly end, int guests, CancellationToken ct = default);
	}
}
