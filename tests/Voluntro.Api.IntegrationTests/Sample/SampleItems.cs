namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>The sample module's "database": a fixed, in-memory list that tests can refer to.</summary>
public static class SampleItems
{
    public static readonly SampleItemDto First =
        new(Guid.Parse("01930000-0000-7000-8000-000000000001"), "First item", SampleItemStatus.Published, "Has a note");

    public static readonly SampleItemDto Second =
        new(Guid.Parse("01930000-0000-7000-8000-000000000002"), "Second item", SampleItemStatus.Draft, null);

    public static IReadOnlyList<SampleItemDto> All { get; } = [First, Second];
}
