namespace ReservationService.Common.Events.Published
{
    public record ReservationRespondedIntegrationEvent(
    Guid GuestId,
    Guid ReservationId,
    string AccommodationName,
    bool IsApproved)
    : IIntegrationEvent;
}
