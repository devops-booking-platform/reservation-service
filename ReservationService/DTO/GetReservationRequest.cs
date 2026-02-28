using ReservationService.Domain;
using ReservationService.Domain.Enums;

namespace ReservationService.DTO;

public class GetReservationRequest : PagedRequest
{
    public ReservationStatus? Status { get; set; }
}