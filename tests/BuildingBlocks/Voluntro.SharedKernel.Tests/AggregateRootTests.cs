namespace Voluntro.SharedKernel.Tests;

public sealed class AggregateRootTests
{
    [Fact]
    public void A_new_aggregate_has_no_domain_events()
    {
        var aggregate = new TestAggregate(TestId.New());

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void Raised_events_are_exposed_in_the_order_they_were_raised()
    {
        var aggregate = new TestAggregate(TestId.New());

        aggregate.Rename("Kari");
        aggregate.Rename("Ola");

        Assert.Equal([new TestRenamed("Kari"), new TestRenamed("Ola")], aggregate.DomainEvents);
    }

    [Fact]
    public void Domain_events_cannot_be_modified_from_outside()
    {
        var aggregate = new TestAggregate(TestId.New());
        aggregate.Rename("Kari");

        var events = Assert.IsAssignableFrom<ICollection<IDomainEvent>>(aggregate.DomainEvents);

        Assert.True(events.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => events.Add(new TestRenamed("Ola")));
        Assert.Throws<NotSupportedException>(events.Clear);
        Assert.Single(aggregate.DomainEvents);
    }

    [Fact]
    public void ClearDomainEvents_removes_all_events()
    {
        var aggregate = new TestAggregate(TestId.New());
        aggregate.Rename("Kari");
        aggregate.Rename("Ola");

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void Events_can_be_read_and_cleared_without_knowing_the_id_type()
    {
        IHasDomainEvents aggregate = new TestAggregate(TestId.New());
        ((TestAggregate)aggregate).Rename("Kari");

        Assert.Single(aggregate.DomainEvents);

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void Raising_null_throws()
    {
        var aggregate = new TestAggregate(TestId.New());

        Assert.Throws<ArgumentNullException>(aggregate.RaiseNull);
    }

    private sealed record TestRenamed(string Name) : IDomainEvent;

    private sealed class TestAggregate(TestId id) : AggregateRoot<TestId>(id)
    {
        public void Rename(string name) => Raise(new TestRenamed(name));

        public void RaiseNull() => Raise(null!);
    }
}
