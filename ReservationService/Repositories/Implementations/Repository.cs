using ReservationService.Data;
using ReservationService.Repositories.Interfaces;

namespace ReservationService.Repositories.Implementations
{
	public class Repository<T>(ApplicationDbContext context) : IRepository<T>
	where T : class
	{
		protected readonly ApplicationDbContext Context = context;

		public async Task AddAsync(T entity) =>
			await Context.Set<T>().AddAsync(entity);

		public void Remove(T entity) =>
			Context.Set<T>().Remove(entity);

		public Task<T?> GetByIdAsync(Guid id) =>
			Context.Set<T>().FindAsync(id).AsTask();

		public IQueryable<T> Query() =>
			Context.Set<T>();
	}
}
