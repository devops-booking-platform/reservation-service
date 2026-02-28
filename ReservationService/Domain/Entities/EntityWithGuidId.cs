namespace ReservationService.Domain.Entities
{
	public abstract class EntityWithGuidId
	{
		public Guid Id { get; private set; } = Guid.NewGuid();
	}
}
