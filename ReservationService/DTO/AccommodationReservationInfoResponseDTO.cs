namespace ReservationService.DTO
{
	public sealed class AccommodationReservationInfoResponseDTO
	{
		public Guid HostId { get; set; }
		public string Name { get; set; } = default!;
		public int MaxGuests { get; set; }
		public decimal TotalPrice { get; set; }
		public bool IsAutoAcceptEnabled { get; set; }
	}
}
