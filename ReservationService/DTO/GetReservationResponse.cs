using ReservationService.Domain.Enums;

namespace ReservationService.DTO;

public class GetReservationResponse
{
    public Guid Id { get; set; }
    public Guid AccommodationId { get; set; }
    public Guid GuestId { get; set; }
    public Guid HostId { get; set; }
    public string AccommodationName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestUsername { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int GuestsCount { get; set; }
    public int TotalPreviousCancellationsByGuest { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalPrice { get; set; }
}