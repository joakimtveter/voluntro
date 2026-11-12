namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// What the API returns for an item. <see cref="Status"/> (an enum) and <see cref="Note"/> (nullable)
/// are here so later tests can check the JSON conventions: enums as camelCase strings, nulls omitted.
/// </summary>
public sealed record SampleItemDto(Guid Id, string Name, SampleItemStatus Status, string? Note);

public enum SampleItemStatus
{
    Draft,
    Published,
}
