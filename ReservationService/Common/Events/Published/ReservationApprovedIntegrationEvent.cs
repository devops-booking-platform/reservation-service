namespace ReservationService.Common.Events.Published
{
    public record ReservationApprovedIntegrationEvent(Guid AccommodationId, Guid ReservationId, DateOnly StartDate, DateOnly EndDate) : IIntegrationEvent;
}
