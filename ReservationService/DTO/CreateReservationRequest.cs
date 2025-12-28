namespace ReservationService.DTO
{
	public class CreateReservationRequest
	{
		public Guid AccommodationId { get; set; }
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
		public int GuestsCount { get; set; }
	}
}
