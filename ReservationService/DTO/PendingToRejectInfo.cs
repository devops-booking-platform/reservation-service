namespace ReservationService.DTO
{
    public record PendingToRejectInfo(Guid GuestId, Guid ReservationId, string AccommodationName);
}
