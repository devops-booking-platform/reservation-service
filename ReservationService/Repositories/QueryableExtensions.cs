using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ReservationService.Domain;
using IConfigurationProvider = AutoMapper.IConfigurationProvider;

namespace ReservationService.Repositories;

public static class QueryableExtensions
{
    public static async Task<PagedResult<TResult>> ToPagedAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        int page,
        int pageSize,
        IConfigurationProvider mapperConfig)
    {
        var totalCount = await query.CountAsync();

        var items = await query
            .ProjectTo<TResult>(mapperConfig)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TResult>
        {
            Items = items,
            TotalCount = totalCount,
            PageSize = pageSize,
            Page = page
        };
    }
}