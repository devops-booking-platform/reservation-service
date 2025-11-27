using ReservationService.Domain.Enums;

namespace ReservationService.Domain.Entities
{
    public class Reservation
    {
        public Guid Id { get; set; }
        public Guid AccommodationId { get; set; }
        public Guid GuestId { get; set; }
        public string GuestEmail { get; set; } = default!;
        public string GuestUsername { get; set; } = default!;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int GuestsCount { get; set; }
        public ReservationStatus Status { get; set; }
    }
}
