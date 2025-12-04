using ReservationService.DTO;

namespace ReservationService.Infrastructure.Clients
{
	public interface IAccommodationClient
	{
		Task<AccommodationReservationInfoResponseDTO> GetAccommodationReservationInfoAsync(Guid id, DateTimeOffset start, DateTimeOffset end, int guests, CancellationToken ct = default);
	}
}
