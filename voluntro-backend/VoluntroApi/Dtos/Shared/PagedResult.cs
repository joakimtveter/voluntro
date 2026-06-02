using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace VoluntroApi.Dtos.Shared;

/// <summary>
/// Represents a single page of results from a paginated query.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class PagedResult<T>
{
    /// <summary>The items on the current page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>The current page number (1-based).</summary>
    public required int CurrentPage { get; init; }

    /// <summary>The number of items per page.</summary>
    public required int PageSize { get; init; }

    /// <summary>The total number of items across all pages.</summary>
    public required int TotalCount { get; init; }

    /// <summary>The total number of pages.</summary>
    public int TotalPages =>
        (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>True if there is a page before the current one.</summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>True if there is a page after the current one.</summary>
    public bool HasNext => CurrentPage < TotalPages;
}

/// <summary>
/// Extension methods for building paginated query results.
/// </summary>
public static class PagingExtensions
{
    /// <summary>
    /// Executes a paginated query and returns a <see cref="PagedResult{TDto}"/>.
    /// Runs two database queries: one for the total count and one for the page data.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried.</typeparam>
    /// <typeparam name="TDto">The DTO type to project results into.</typeparam>
    /// <param name="query">The base queryable, with filters and ordering already applied.</param>
    /// <param name="selector">Projection from entity to DTO.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged result containing the projected items and pagination metadata.</returns>
    public static async Task<PagedResult<TDto>> ToPagedResultAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        Expression<Func<TEntity, TDto>> selector,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Select(selector)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TDto>
        {
            Items = items,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}