namespace ReservationService.DTO
{
	public class GuestApprovedReservationResponseDTO
	{
		public Guid Id { get; set; }
		public string AccommodationName { get; set; } = "";
		public DateTimeOffset StartDate { get; set; }
		public DateTimeOffset EndDate { get; set; }
		public decimal TotalPrice { get; set; }
		public int GuestsCount { get; set; }
	}
}
