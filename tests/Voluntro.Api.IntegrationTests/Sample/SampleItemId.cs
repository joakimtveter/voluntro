using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public readonly record struct SampleItemId(Guid Value) : IEntityId<SampleItemId>
{
    public static SampleItemId New() => new(Guid.CreateVersion7());

    public static SampleItemId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
