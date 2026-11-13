namespace Voluntro.BuildingBlocks.Contracts;

/// <summary>
/// One page of a list, serialized as <c>{ items, page, pageSize, totalCount }</c>.
/// <see cref="Page"/> and <see cref="PageSize"/> echo the (clamped) <see cref="PageRequest"/>;
/// <see cref="TotalCount"/> counts every matching item, not just this page's.
/// </summary>
public sealed record PagedList<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
