using ReservationService.Common.Exceptions;
using ReservationService.DTO;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace ReservationService.Infrastructure.Clients
{
	public sealed class AccommodationClient(HttpClient http) : IAccommodationClient
	{
		public async Task<AccommodationReservationInfoResponseDTO> GetAccommodationReservationInfoAsync(Guid id, DateOnly start, DateOnly end, int guests, CancellationToken ct = default)
		{
			var url = $"/api/accommodations/{id}/reservation-info?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}&guests={guests}";
			var resp = await http.GetAsync(url, ct);

			if (resp.IsSuccessStatusCode)
			{
				var dto = await resp.Content.ReadFromJsonAsync<AccommodationReservationInfoResponseDTO>(cancellationToken: ct);
				return dto ?? throw new ExternalServiceException("AccommodationService returned empty response.");
			}
			if (resp.StatusCode == HttpStatusCode.NotFound)
				throw new NotFoundException("Accommodation not found.");

			if (resp.StatusCode == HttpStatusCode.Conflict)
				throw new ConflictException("Accommodation is not available for the selected dates.");

			if (resp.StatusCode == HttpStatusCode.BadRequest)
				throw new ValidationException("Invalid reservation input (dates/guests).");

			if (resp.StatusCode == HttpStatusCode.Unauthorized)
				throw new ExternalServiceException("AccommodationService unauthorized.");

			if (resp.StatusCode == HttpStatusCode.Forbidden)
				throw new ExternalServiceException("AccommodationService forbidden.");

			throw new ExternalServiceException($"AccommodationService error ({(int)resp.StatusCode}).");
		}
	}
}
