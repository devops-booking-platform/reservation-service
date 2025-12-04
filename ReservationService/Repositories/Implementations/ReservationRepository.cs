using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;
using ReservationService.Repositories.Interfaces;

namespace ReservationService.Repositories.Implementations
{
	public class ReservationRepository(ApplicationDbContext context) : Repository<Reservation>(context), IReservationRepository
	{
		public Task<int> RejectOverlappingPendingAsync(Guid accommodationId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct)
		{
			return Context.Reservations
				.Where(r => r.AccommodationId == accommodationId
							&& r.StartDate < endDate
							&& r.EndDate > startDate
							&& r.Status == ReservationStatus.Pending)
				.ExecuteUpdateAsync(setters => setters
					.SetProperty(r => r.Status, ReservationStatus.Rejected), ct);
		}

		public Task<bool> HasOverlappingApprovedReservationAsync(Guid accommodationId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default)
		{
			return Context.Reservations.AnyAsync(x =>
			x.AccommodationId == accommodationId &&
			x.StartDate < endDate &&
			x.EndDate > startDate &&
			x.Status == ReservationStatus.Approved
			, ct);
		}
		public Task AcquireAccommodationLockAsync(Guid accommodationId, CancellationToken ct = default)
		{
			return Context.Database.ExecuteSqlInterpolatedAsync($@"
			DECLARE @result int;

			EXEC @result = sp_getapplock
				@Resource   = {"reservation:" + accommodationId},
				@LockMode   = 'Exclusive',
				@LockOwner  = 'Transaction',
				@LockTimeout = 3000;

			IF (@result < 0)
				THROW 50001, 'Could not acquire reservation lock (timeout).', 1;
			", ct);
		}

		public Task<bool> ExistsByIdempotencyKey(Guid guestId, Guid idempotencyKey, CancellationToken ct = default)
		{
			return Context.Reservations.AnyAsync(x => x.IdempotencyKey == idempotencyKey && x.GuestId == guestId, ct);
		}
	}
}
