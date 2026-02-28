namespace ReservationService.Common.Events.Published
{
    public record ReservationCanceledIntegrationEvent(
    Guid HostId,
    Guid ReservationId,
    Guid AccommodationId,
    string AccommodationName,
    DateOnly StartDate,
    DateOnly EndDate,
    string GuestUsername) : IIntegrationEvent;
}
