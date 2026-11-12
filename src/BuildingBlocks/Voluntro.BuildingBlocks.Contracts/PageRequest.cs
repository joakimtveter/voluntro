namespace Voluntro.BuildingBlocks.Contracts;

/// <summary>
/// A request for one page of a list: <c>?page=1&amp;pageSize=25&amp;sort=lastName:asc&amp;search=…</c>.
/// Out-of-range values are clamped here, so a handler never sees an invalid page.
/// <see cref="Sort"/> and <see cref="Search"/> are passed through as-is until the first real list endpoint
/// defines the sort grammar and its allowed fields.
/// </summary>
/// <remarks>
/// MVC binds a record through its primary constructor, matching parameter names to property names exactly
/// (case-sensitive), and uses the parameter defaults for missing query values. <see cref="Page"/> and
/// <see cref="PageSize"/> are redeclared get-only with clamping initializers, so not even a <c>with</c>
/// expression can bypass the clamping.
/// </remarks>
public sealed record PageRequest(int Page = 1, int PageSize = PageRequest.DefaultPageSize, string? Sort = null, string? Search = null)
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    /// <summary>1-based page number; at least 1.</summary>
    public int Page { get; } = Math.Max(Page, 1);

    /// <summary>Items per page; between 1 and <see cref="MaxPageSize"/>.</summary>
    public int PageSize { get; } = Math.Clamp(PageSize, 1, MaxPageSize);
}
