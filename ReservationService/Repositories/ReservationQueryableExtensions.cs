using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;

namespace ReservationService.Repositories;

public static class ReservationQueryableExtensions
{
    public static IQueryable<Reservation>
        QueryByStatus(this IQueryable<Reservation> query, ReservationStatus? status) =>
        status.HasValue ? query.Where(x => x.Status == status.Value) : query;

    public static IQueryable<Reservation> QueryByUser(this IQueryable<Reservation> query, Guid userId, string role) =>
        role.Equals("host", StringComparison.CurrentCultureIgnoreCase)
            ? query.Where(x => x.HostId == userId)
            : query.Where(x => x.GuestId == userId);
}