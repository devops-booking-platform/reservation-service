namespace ReservationService.DTO
{
    public class HostPendingReservationResponseDTO
    {
        public Guid ReservationId { get; set; }
        public string AccommodationName { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public int GuestsCount { get; set; }
    }
}
