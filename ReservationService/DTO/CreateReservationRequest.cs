namespace ReservationService.DTO
{
	public class CreateReservationRequest
	{
		public Guid AccommodationId { get; set; }
		public DateTimeOffset StartDate { get; set; }
		public DateTimeOffset EndDate { get; set; }
		public int GuestsCount { get; set; }
	}
}
