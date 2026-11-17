using Voluntro.BuildingBlocks.Application;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Only the database assigns a version, so an unsaved aggregate (version 0) is all a unit test can have; the
/// integration tests cover saved versions.
/// </summary>
public sealed class AggregateVersionExtensionsTests
{
    [Fact]
    public void The_version_the_client_read_passes()
    {
        var thing = new Thing(ThingId.New());

        Assert.True(thing.EnsureVersion(0).IsSuccess);
    }

    [Fact]
    public void Any_other_version_is_a_concurrency_conflict()
    {
        var thing = new Thing(ThingId.New());

        var result = thing.EnsureVersion(7);

        Assert.Equal(CommonErrors.ConcurrencyConflict.Code, result.Error?.Code);
        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Contains(thing.Id.ToString(), result.Error?.Detail, StringComparison.Ordinal);
    }

    private readonly record struct ThingId(Guid Value) : IEntityId<ThingId>
    {
        public static ThingId New() => new(Guid.CreateVersion7());

        public static ThingId From(Guid value) => new(value);

        public override string ToString() => Value.ToString();
    }

    private sealed class Thing(ThingId id) : AggregateRoot<ThingId>(id);
}
