namespace Voluntro.SharedKernel.Tests;

public readonly record struct TestId(Guid Value) : IEntityId<TestId>
{
    public static TestId New() => new(Guid.CreateVersion7());

    public static TestId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
