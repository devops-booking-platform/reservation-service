using ReservationService.Data;
using ReservationService.Repositories.Interfaces;

namespace ReservationService.Repositories.Implementations
{
	public class UnitOfWork(
	ApplicationDbContext context)
	: IUnitOfWork
	{
		public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
			context.SaveChangesAsync(cancellationToken);
	}
}
