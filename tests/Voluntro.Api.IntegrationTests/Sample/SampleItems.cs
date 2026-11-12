namespace Voluntro.Api.IntegrationTests.Sample;

/// <summary>
/// The sample module's "database": a fixed, in-memory list that tests can refer to.
/// Thirty items, so a default page (25) doesn't hold them all.
/// </summary>
public static class SampleItems
{
    public static readonly SampleItemDto First = new(IdFor(1), "First item", SampleItemStatus.Published, "Has a note");

    public static readonly SampleItemDto Second = new(IdFor(2), "Second item", SampleItemStatus.Draft, null);

    public static IReadOnlyList<SampleItemDto> All { get; } =
        [First, Second, .. Enumerable.Range(3, 28).Select(Numbered)];

    /// <summary>Deterministic, time-ordered-looking IDs: item 1 is …000000000001, item 30 is …00000000001e.</summary>
    public static Guid IdFor(int number) => Guid.Parse($"01930000-0000-7000-8000-{number:x12}");

    private static SampleItemDto Numbered(int number) =>
        new(IdFor(number), $"Item {number:00}", number % 2 == 0 ? SampleItemStatus.Draft : SampleItemStatus.Published, null);
}
