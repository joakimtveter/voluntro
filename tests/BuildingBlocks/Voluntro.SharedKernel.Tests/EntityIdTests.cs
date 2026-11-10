namespace Voluntro.SharedKernel.Tests;

public sealed class EntityIdTests
{
    [Fact]
    public void Ids_with_the_same_value_are_equal()
    {
        var value = Guid.CreateVersion7();

        var first = TestId.From(value);
        var second = TestId.From(value);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Ids_with_different_values_are_not_equal()
    {
        Assert.NotEqual(TestId.New(), TestId.New());
    }

    [Fact]
    public void New_creates_a_version_7_guid()
    {
        var id = TestId.New();

        Assert.Equal(7, id.Value.Version);
    }

    [Fact]
    public void Ids_created_later_sort_after_earlier_ones()
    {
        // Guid v7 is only ordered across milliseconds, so use explicit timestamps
        // instead of two New() calls that may land in the same millisecond.
        var earlier = TestId.From(Guid.CreateVersion7(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)));
        var later = TestId.From(Guid.CreateVersion7(new DateTimeOffset(2026, 1, 1, 12, 0, 0, 1, TimeSpan.Zero)));

        Assert.True(earlier.Value.CompareTo(later.Value) < 0);
    }

    [Fact]
    public void New_uses_the_current_time()
    {
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var id = TestId.New();
        var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var timestamp = UnixMillisecondsOf(id.Value);

        Assert.InRange(timestamp, before, after);
    }

    [Fact]
    public void From_accepts_an_empty_guid()
    {
        var id = TestId.From(Guid.Empty);

        Assert.Equal(Guid.Empty, id.Value);
    }

    [Fact]
    public void ToString_returns_the_bare_guid()
    {
        var value = Guid.CreateVersion7();

        Assert.Equal(value.ToString(), TestId.From(value).ToString());
    }

    [Fact]
    public void Generic_code_can_create_and_rehydrate_ids()
    {
        var created = Create<TestId>();
        var rehydrated = Rehydrate<TestId>(created.Value);

        Assert.Equal(created, rehydrated);
    }

    private static TId Create<TId>() where TId : struct, IEntityId<TId> => TId.New();

    private static TId Rehydrate<TId>(Guid value) where TId : struct, IEntityId<TId> => TId.From(value);

    // The first 48 bits of a v7 Guid are the Unix timestamp in milliseconds, big-endian.
    private static long UnixMillisecondsOf(Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes, bigEndian: true, out _);

        long milliseconds = 0;
        for (var i = 0; i < 6; i++)
        {
            milliseconds = (milliseconds << 8) | bytes[i];
        }

        return milliseconds;
    }
}
