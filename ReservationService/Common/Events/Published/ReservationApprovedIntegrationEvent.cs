namespace ReservationService.Common.Events.Published
{
    public record ReservationApprovedIntegrationEvent(Guid AccommodationId, Guid ReservationId, DateTimeOffset StartDate, DateTimeOffset EndDate) : IIntegrationEvent;
}
