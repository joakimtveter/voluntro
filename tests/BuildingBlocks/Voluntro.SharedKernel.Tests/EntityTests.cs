namespace Voluntro.SharedKernel.Tests;

public sealed class EntityTests
{
    [Fact]
    public void Constructor_sets_the_id()
    {
        var id = TestId.New();

        var entity = new TestEntity(id, "Kari");

        Assert.Equal(id, entity.Id);
    }

    [Fact]
    public void Constructor_rejects_an_empty_id()
    {
        var exception = Assert.Throws<ArgumentException>(() => new TestEntity(TestId.From(Guid.Empty), "Kari"));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void Entities_with_the_same_id_are_equal_regardless_of_other_state()
    {
        var id = TestId.New();
        var first = new TestEntity(id, "Kari");
        var second = new TestEntity(id, "Ola");

        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var first = new TestEntity(TestId.New(), "Kari");
        var second = new TestEntity(TestId.New(), "Kari");

        Assert.False(first.Equals(second));
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void Entities_of_different_types_with_the_same_id_are_not_equal()
    {
        var id = TestId.New();
        var entity = new TestEntity(id, "Kari");
        var other = new OtherTestEntity(id);

        Assert.False(entity.Equals(other));
        Assert.False(other.Equals(entity));
        Assert.False(entity == other);
        Assert.True(entity != other);
    }

    [Fact]
    public void A_derived_entity_is_not_equal_to_its_base_type_with_the_same_id()
    {
        var id = TestId.New();
        var entity = new TestEntity(id, "Kari");
        var derived = new DerivedTestEntity(id, "Kari");

        Assert.False(entity.Equals(derived));
        Assert.False(derived.Equals(entity));
    }

    [Fact]
    public void An_entity_is_not_equal_to_null()
    {
        var entity = new TestEntity(TestId.New(), "Kari");

        Assert.False(entity.Equals(null));
        Assert.False(entity.Equals((object?)null));
        Assert.False(entity == null);
        Assert.False(null == entity);
        Assert.True(entity != null);
    }

    [Fact]
    public void Two_null_entities_are_equal()
    {
        TestEntity? first = null;
        TestEntity? second = null;

        Assert.True(first == second);
        Assert.False(first != second);
    }

    [Fact]
    public void An_entity_is_not_equal_to_a_non_entity()
    {
        var id = TestId.New();
        var entity = new TestEntity(id, "Kari");

        Assert.False(entity.Equals(id));
        Assert.False(entity.Equals("Kari"));
    }

    private class TestEntity(TestId id, string name) : Entity<TestId>(id)
    {
        public string Name { get; } = name;
    }

    private sealed class DerivedTestEntity(TestId id, string name) : TestEntity(id, name);

    private sealed class OtherTestEntity(TestId id) : Entity<TestId>(id);
}
