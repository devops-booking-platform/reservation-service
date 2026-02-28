namespace ReservationService.Common.Events
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken ct = default)
            where T : IIntegrationEvent;
    }
}
