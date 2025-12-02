using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;
using ReservationService.Repositories.Interfaces;

namespace ReservationService.Repositories.Implementations
{
	public class ReservationRepository(ApplicationDbContext context) : Repository<Reservation>(context), IReservationRepository
	{
		public Task<bool> HasOverlappingApprovedReservationAsync(Guid accommodationId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
		{
			return Context.Reservations.AnyAsync(x =>
			x.AccommodationId == accommodationId &&
			x.StartDate < endDate &&
			x.EndDate > startDate &&
			x.Status == ReservationStatus.Approved
			, ct);
		}
	}
}
