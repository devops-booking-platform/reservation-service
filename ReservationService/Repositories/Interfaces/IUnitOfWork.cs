using Microsoft.EntityFrameworkCore.Storage;

namespace ReservationService.Repositories.Interfaces
{
	public interface IUnitOfWork
	{
		Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
		Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
	}
}
