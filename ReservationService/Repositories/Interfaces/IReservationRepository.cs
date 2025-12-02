using ReservationService.Domain.Entities;

namespace ReservationService.Repositories.Interfaces
{
	public interface IReservationRepository : IRepository<Reservation>
	{
		Task<bool> HasOverlappingApprovedReservationAsync(Guid accommodationId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
	}
}
