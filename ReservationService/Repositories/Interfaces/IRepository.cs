namespace ReservationService.Repositories.Interfaces
{
	public interface IRepository<T> where T : class
	{
		Task AddAsync(T entity);
		Task<T?> GetByIdAsync(Guid id);
		void Remove(T entity);
		IQueryable<T> Query();
	}
}
