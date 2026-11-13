using Voluntro.SharedKernel;

namespace Voluntro.Api.IntegrationTests.Sample;

public readonly record struct SampleOrderLineId(Guid Value) : IEntityId<SampleOrderLineId>
{
    public static SampleOrderLineId New() => new(Guid.CreateVersion7());

    public static SampleOrderLineId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
