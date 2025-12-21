namespace ReservationService.Common.Events.Published
{
    public record ReservationCanceledIntegrationEvent(Guid AccommodationId, Guid ReservationId) : IIntegrationEvent;
}
