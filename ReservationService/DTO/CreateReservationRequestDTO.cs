namespace ReservationService.DTO
{
	public class CreateReservationRequestDTO
	{
		public Guid AccommodationId { get; set; }
		public DateTimeOffset StartDate { get; set; }
		public DateTimeOffset EndDate { get; set; }
		public int GuestsCount { get; set; }
	}
}
